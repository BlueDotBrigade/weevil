namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using BlueDotBrigade.Weevil.IO;
	using Diagnostics;

	/// <remarks>
	/// A repository over a file reads once: <see cref="Get(Range, int)"/> completes the source of the bytes read before it returns the records.
	/// In disk mode, the records read their content back through it (#1025). In both modes, it holds the checksums the saved index keeps (#1023).
	/// </remarks>
	internal class SerializedRecordRepository : IRecordRepository
	{
		public const bool LoggingEnabled = true;

		public const int FirstRecordByteOffset = 0;
		public const int FirstRecordLineNumber = 1;

		private readonly Stream _log;
		private readonly StorageMode _storageMode;
		private readonly long _firstRecordByteOffset;
		private readonly int _firstRecordLineNumber;
		private readonly LogLineReader _reader;
		private readonly MultilineRecordParser _recordParser;

		public SerializedRecordRepository(Stream log, IRecordParser recordParser)
			: this(log, recordParser, MultilineRecordParser.MaximumLinesToSearch)
		{
			// nothing to do
		}

		/// <param name="maximumLinesToSearch">Represents the number of lines that will be read while searching for the first record.</param>
		/// <param name="storageMode">Whether the records keep their content, or read it back from the log, which must then be a file.</param>
		public SerializedRecordRepository(Stream log, IRecordParser recordParser, int maximumLinesToSearch, StorageMode storageMode = StorageMode.Ram)
			: this(log, recordParser, FirstRecordByteOffset, FirstRecordLineNumber, LoggingEnabled, maximumLinesToSearch, storageMode)
		{
			// nothing to do
		}

		/// <param name="storageMode">Whether the records keep their content, or read it back from the log, which must then be a file.</param>
		/// <param name="checksumStartOffset">Where the checksums of the bytes read start, at or before <paramref name="firstRecordByteOffset"/>: by default, there.</param>
		public SerializedRecordRepository(Stream log, IRecordParser recordParser, long firstRecordByteOffset, int firstRecordLineNumber, bool loggingEnabled, int maximumLinesToSearch = MultilineRecordParser.MaximumLinesToSearch, StorageMode storageMode = StorageMode.Ram, long checksumStartOffset = -1)
		{
			_log = log;
			_storageMode = storageMode;
			_firstRecordByteOffset = firstRecordByteOffset;
			_firstRecordLineNumber = firstRecordLineNumber;

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Preparing to read records from disk. {nameof(firstRecordByteOffset)}={firstRecordByteOffset}, {nameof(firstRecordLineNumber)}={firstRecordLineNumber}");

			_reader = new LogLineReader(_log, _firstRecordByteOffset, storageMode: storageMode, checksumStartOffset: checksumStartOffset);
			_recordParser = new MultilineRecordParser(
				_reader,
				recordParser,
				maximumLinesToSearch,
				firstRecordLineNumber,
				loggingEnabled);
		}

		/// <summary>
		/// The file's path and the checksum of each block read, in both modes, once <see cref="Get(Range, int)"/> has read the log,
		/// so they can be saved with the log's index (#1023). On disk, the records read their content back through it (#1025).
		/// </summary>
		/// <remarks>
		/// <see langword="null"/> for a log read from a stream that is not a file, which only RAM mode accepts.
		/// </remarks>
		internal LogFileSource Source { get; private set; }

		public IRecord GetNext()
		{
			return _recordParser.GetNext();
		}

		public RecordList GetAll()
		{
			return Get(int.MaxValue);
		}

		public RecordList Get(int maximumCount)
		{
			return Get(Range.All, maximumCount);
		}

		/// <remarks>
		/// The records go into a record table, 21 bytes each, instead of a growing list of records (#1138).
		/// The list returned creates each record when it is read (#1140).
		/// </remarks>
		public RecordList Get(Range range, int maximumCount)
		{
			var table = new RecordTable.Builder();

			if (range.Equals(Range.All))
			{
				range = new Range(1, int.MaxValue);
			}

			bool hasRecord;

			do
			{
				// A record on the dummy's line ends the read, as it always has: a plugin's own record type can be on line -1.
				hasRecord = _recordParser.TryReadNext(out MultilineRecordParser.ParsedRecord record) &&
				            record.LineNumber != Record.Dummy.LineNumber;

				// Records are read in line order, so the rest of the log is not read once the range ends, as when one region is opened (#1213).
				if (hasRecord && record.LineNumber > range.End.Value)
				{
					break;
				}

				if (hasRecord && range.Start.Value <= record.LineNumber)
				{
					record.AddTo(table);
				}
			} while (hasRecord && table.Count < maximumCount);

			// In disk mode, the records read their content back through the source, which can be read only once complete.
			this.Source = _reader.BuildSource();

			return new TableRecordList(table.Build());
		}
	}
}