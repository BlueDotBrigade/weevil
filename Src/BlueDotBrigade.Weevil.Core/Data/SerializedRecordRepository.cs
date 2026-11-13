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

		private readonly Stream _source;
		private readonly StorageMode _storageMode;
		private readonly long _firstRecordByteOffset;
		private readonly int _firstRecordLineNumber;
		private readonly LogLineReader _reader;
		private readonly MultilineRecordParser _recordParser;

		public SerializedRecordRepository(Stream source, IRecordParser recordParser)
			: this(source, recordParser, MultilineRecordParser.MaximumLinesToSearch)
		{
			// nothing to do
		}

		/// <param name="maximumLinesToSearch">Represents the number of lines that will be read while searching for the first record.</param>
		/// <param name="storageMode">Whether the records keep their content, or read it back from the log, which must then be a file.</param>
		public SerializedRecordRepository(Stream source, IRecordParser recordParser, int maximumLinesToSearch, StorageMode storageMode = StorageMode.Ram)
			: this(source, recordParser, FirstRecordByteOffset, FirstRecordLineNumber, LoggingEnabled, maximumLinesToSearch, storageMode)
		{
			// nothing to do
		}

		/// <param name="storageMode">Whether the records keep their content, or read it back from the log, which must then be a file.</param>
		public SerializedRecordRepository(Stream source, IRecordParser recordParser, long firstRecordByteOffset, int firstRecordLineNumber, bool loggingEnabled, int maximumLinesToSearch = MultilineRecordParser.MaximumLinesToSearch, StorageMode storageMode = StorageMode.Ram)
		{
			_source = source;
			_storageMode = storageMode;
			_firstRecordByteOffset = firstRecordByteOffset;
			_firstRecordLineNumber = firstRecordLineNumber;

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Preparing to read records from disk. {nameof(firstRecordByteOffset)}={firstRecordByteOffset}, {nameof(firstRecordLineNumber)}={firstRecordLineNumber}");

			_reader = new LogLineReader(_source, _firstRecordByteOffset, storageMode: storageMode);
			_recordParser = new MultilineRecordParser(
				_reader,
				recordParser,
				maximumLinesToSearch,
				firstRecordLineNumber,
				loggingEnabled);
		}

		/// <summary>
		/// In disk mode, the source the records read their content back through, once <see cref="Get(Range, int)"/> has read the log: otherwise <see langword="null"/>.
		/// </summary>
		internal LogFileSource Source { get; private set; }

		/// <summary>
		/// The bytes read, with a checksum of each block, in both modes, once <see cref="Get(Range, int)"/> has read the log,
		/// so they can be saved with the log's index (#1023). <see langword="null"/> when the log is not a file.
		/// </summary>
		/// <remarks>
		/// Only disk mode reads content back through it: in RAM mode the records keep their content, so it is not the engine's <see cref="Source"/>.
		/// </remarks>
		internal LogFileSource ParsedSource { get; private set; }

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

				if (hasRecord &&
				    range.Start.Value <= record.LineNumber &&
				    record.LineNumber <= range.End.Value)
				{
					record.AddTo(table);
				}
			} while (hasRecord && table.Count < maximumCount);

			// In disk mode, the records read their content back through the source, which can be read only once complete.
			this.ParsedSource = _reader.BuildSource();
			this.Source = _storageMode == StorageMode.Disk ? this.ParsedSource : null;

			return new TableRecordList(table.Build());
		}
	}
}