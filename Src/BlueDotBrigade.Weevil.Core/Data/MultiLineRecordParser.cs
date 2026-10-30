namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using BlueDotBrigade.Weevil.IO;
	using Diagnostics;
	using Log = Diagnostics.Log;

	internal class MultilineRecordParser
	{
		/// <summary>
		/// Represents the number of lines of a file that will be read while searching for the beginning of a new record.
		/// </summary>
		public const int MaximumLinesToSearch = 256;

		public const int FirstRecordLineNumber = 1;

		public const bool LoggingEnabled = true;

		private readonly LogLineReader _dataSource;
		private readonly IRecordParser _recordParser;
		private readonly int _maximumLinesToSearch;
		private readonly bool _isLoggingEnabled;
		private int _lineNumber;

		// The first line of the next record, found while reading the continuation lines of the one before it.
		private RecordStart _nextRecord;

		// The records read since the current chunk started, which are created once the chunk is complete (#1133).
		private readonly List<ReadRecord> _unchunked = new List<ReadRecord>();
		private readonly Queue<IRecord> _ready = new Queue<IRecord>();
		private bool _isFinished;

		public MultilineRecordParser(LogLineReader dataSource, IRecordParser recordParser)
			: this(dataSource, recordParser, MaximumLinesToSearch, FirstRecordLineNumber, LoggingEnabled)
		{
			// nothing to do
		}

		public MultilineRecordParser(LogLineReader dataSource, IRecordParser recordParser, int maximumLinesToSearch, int firstRecordLineNumber, bool isLoggingEnabled)
		{
			_maximumLinesToSearch = maximumLinesToSearch > 0 ? maximumLinesToSearch : throw new ArgumentOutOfRangeException(
				 nameof(maximumLinesToSearch),
				 maximumLinesToSearch,
				 "Value is expected to be greater than zero.");

			_lineNumber = firstRecordLineNumber - 1;
			_dataSource = dataSource;
			_recordParser = recordParser;
			_isLoggingEnabled = isLoggingEnabled;
		}

		/// <summary>
		/// Returns the next record, or <see cref="Record.Dummy"/> when there are no more.
		/// </summary>
		/// <remarks>
		/// Records are created a chunk at a time, so the log is read up to a chunk ahead of the record returned.
		/// </remarks>
		public IRecord GetNext()
		{
			while (_ready.Count == 0 && !_isFinished)
			{
				ReadNextRecord();
			}

			return _ready.Count > 0 ? _ready.Dequeue() : Record.Dummy;
		}

		private void ReadNextRecord()
		{
			if (BeginReadNext(out RecordStart current))
			{
				EndReadNext(current);
			}
			else
			{
				var isDataAvailable = _dataSource.TryReadLine(out _, out _, out _);

				if (_isLoggingEnabled)
				{
					if (isDataAvailable)
					{
						Log.Default.Write(
							LogSeverityType.Warning, string.Format(CultureInfo.InvariantCulture,
								$"Unable to find the beginning of a record. CurrentStreamPosition={_dataSource.Position}"));
					}
					else
					{
						Log.Default.Write(
							LogSeverityType.Debug,
							"The end of file (EOF) has been reached.");
					}
				}

				CompleteChunk(_dataSource.Position);
				_isFinished = true;
			}
		}

		private bool BeginReadNext(out RecordStart current)
		{
			if (_nextRecord.Record != null)
			{
				current = _nextRecord;
				_nextRecord = default;
				return true;
			}

			var linesRead = 0;

			while (linesRead < _maximumLinesToSearch && _dataSource.TryReadLine(out var offset, out var length, out var line))
			{
				linesRead++;
				_lineNumber++;

				// Get beginning of current record
				if (_recordParser.TryParse(_lineNumber, line, out IRecord record))
				{
					// A parser that returns no genuine record has not found one: the search goes on, as it always has.
					if (Record.IsGenuine(record))
					{
						current = new RecordStart(record, line, offset, length, _dataSource.IsQuickToDecode);
						return true;
					}

					continue;
				}

				if (_isLoggingEnabled)
				{
					Log.Default.Write(
						LogSeverityType.Error, string.Format(CultureInfo.InvariantCulture,
							"The record appears to be corrupt, or incomplete. LineNumber={0}", _lineNumber));
				}
			}

			current = default;
			return false;
		}

		private void EndReadNext(RecordStart current)
		{
			var end = current.Offset + current.Length;
			var isMultiLine = false;

			// Built only for a record that keeps a string, and only when continuation lines are found, because most records are a single line.
			StringBuilder multilineContent = null;

			while (_dataSource.TryReadLine(out var offset, out var length, out var line))
			{
				_lineNumber++;

				// Have we found the next record?
				if (_recordParser.TryParse(_lineNumber, line, out IRecord record))
				{
					// Without a genuine record, the line still ends this record, but starts none, as it always has.
					if (Record.IsGenuine(record))
					{
						_nextRecord = new RecordStart(record, line, offset, length, _dataSource.IsQuickToDecode);
					}

					break;
				}

				isMultiLine = true;
				end = offset + length;

				if (!current.CanKeepBytes)
				{
					multilineContent ??= new StringBuilder(current.Record.Content);
					multilineContent.Append("\r\n");
					multilineContent.Append(line);
				}
			}

			_unchunked.Add(current.CanKeepBytes
				? new ReadRecord(current.Record, current.Offset, (int)(end - current.Offset), isMultiLine)
				: new ReadRecord(KeepString(current.Record, multilineContent)));

			if (_nextRecord.Record == null)
			{
				CompleteChunk(_dataSource.Position);
			}
			else if (_dataSource.IsChunkFull(_nextRecord.Offset))
			{
				CompleteChunk(_nextRecord.Offset);
			}
		}

		/// <summary>
		/// Finalizes a record that keeps its content as a string, as a plugin's own record type or changed text needs.
		/// </summary>
		/// <remarks>
		/// A single-line <see cref="Record"/> is kept as the parser created it; it is immutable, so no copy is needed.
		/// A multi-line record is created again, because whether it is multi-line is fixed when a record is created (#1012).
		/// </remarks>
		private static IRecord KeepString(IRecord record, StringBuilder multilineContent)
		{
			if (multilineContent != null || !(record is Record))
			{
				return new Record(
					 record.LineNumber,
					 record.CreatedAt,
					 record.Severity,
					 multilineContent?.ToString() ?? record.Content,
					 isMultiLine: multilineContent != null,
					 record.WasGeneratedByUi);
			}

			return record;
		}

		/// <summary>
		/// Ends the current chunk at <paramref name="endOffset"/>, and creates the records read since it started, in order.
		/// </summary>
		private void CompleteChunk(long endOffset)
		{
			ContentChunk chunk = null;

			foreach (ReadRecord read in _unchunked)
			{
				if (read.Offset >= 0)
				{
					chunk ??= _dataSource.TakeChunk(endOffset);

					_ready.Enqueue(new Record(
						read.LineNumber,
						read.CreatedAt,
						read.Severity,
						chunk,
						(int)(read.Offset - chunk.FileOffset),
						read.Length,
						read.IsMultiLine,
						read.WasGeneratedByUi));
				}
				else
				{
					_ready.Enqueue(read.Record);
				}
			}

			if (chunk == null)
			{
				_dataSource.SkipChunk(endOffset);
			}

			_unchunked.Clear();
		}

		/// <summary>
		/// The first line of a record, and the record the parser made from it.
		/// </summary>
		private readonly struct RecordStart
		{
			public RecordStart(IRecord record, string line, long offset, int length, bool isQuickToDecode)
			{
				this.Record = record;
				this.Offset = offset;
				this.Length = length;

				// The bytes can stand in for the text only when the parser kept the line as it is, and the log decodes quickly.
				this.CanKeepBytes = isQuickToDecode && record is Record && string.Equals(record.Content, line, StringComparison.Ordinal);
			}

			public IRecord Record { get; }

			public long Offset { get; }

			public int Length { get; }

			public bool CanKeepBytes { get; }
		}

		/// <summary>
		/// A record that has been read: either where its bytes are, with what the parser found, or a record that keeps its string.
		/// </summary>
		private readonly struct ReadRecord
		{
			public ReadRecord(IRecord parsed, long offset, int length, bool isMultiLine)
			{
				this.Record = null;
				this.LineNumber = parsed.LineNumber;
				this.CreatedAt = parsed.CreatedAt;
				this.Severity = parsed.Severity;
				this.WasGeneratedByUi = parsed.WasGeneratedByUi;
				this.Offset = offset;
				this.Length = length;
				this.IsMultiLine = isMultiLine;
			}

			public ReadRecord(IRecord record)
			{
				this.Record = record;
				this.Offset = -1;
				this.Length = 0;
				this.IsMultiLine = record.IsMultiLine;
			}

			// Only for a record that keeps its string; for the others, only what the parser found is kept, so its line's text can be collected (#1133).
			public IRecord Record { get; }

			public int LineNumber { get; }

			public DateTime CreatedAt { get; }

			public SeverityType Severity { get; }

			public bool WasGeneratedByUi { get; }

			// -1 when the record keeps its string.
			public long Offset { get; }

			public int Length { get; }

			public bool IsMultiLine { get; }
		}
	}
}
