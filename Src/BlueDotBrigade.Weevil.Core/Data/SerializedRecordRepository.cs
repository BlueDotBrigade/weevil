namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using BlueDotBrigade.Weevil.IO;
	using Diagnostics;

	internal class SerializedRecordRepository : IRecordRepository
	{
		public const bool LoggingEnabled = true;

		public const int FirstRecordByteOffset = 0;
		public const int FirstRecordLineNumber = 1;

		private readonly Stream _source;
		private readonly long _firstRecordByteOffset;
		private readonly int _firstRecordLineNumber;
		private readonly MultilineRecordParser _recordParser;

		public SerializedRecordRepository(Stream source, IRecordParser recordParser)
			: this(source, recordParser, MultilineRecordParser.MaximumLinesToSearch)
		{
			// nothing to do
		}

		/// <param name="maximumLinesToSearch">Represents the number of lines that will be read while searching for the first record.</param>
		public SerializedRecordRepository(Stream source, IRecordParser recordParser, int maximumLinesToSearch)
			: this(source, recordParser, FirstRecordByteOffset, FirstRecordLineNumber, LoggingEnabled, maximumLinesToSearch)
		{
			// nothing to do
		}

		public SerializedRecordRepository(Stream source, IRecordParser recordParser, long firstRecordByteOffset, int firstRecordLineNumber, bool loggingEnabled, int maximumLinesToSearch = MultilineRecordParser.MaximumLinesToSearch)
		{
			_source = source;
			_firstRecordByteOffset = firstRecordByteOffset;
			_firstRecordLineNumber = firstRecordLineNumber;

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Preparing to read records from disk. {nameof(firstRecordByteOffset)}={firstRecordByteOffset}, {nameof(firstRecordLineNumber)}={firstRecordLineNumber}");

			_recordParser = new MultilineRecordParser(
				new LogLineReader(_source, _firstRecordByteOffset),
				recordParser,
				maximumLinesToSearch,
				firstRecordLineNumber,
				loggingEnabled);
		}

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
		/// Core still holds an array of records, created from the table, until records are created when they are read (#1140).
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
				hasRecord = _recordParser.TryReadNext(out MultilineRecordParser.ParsedRecord record);

				if (hasRecord &&
				    range.Start.Value <= record.LineNumber &&
				    record.LineNumber <= range.End.Value)
				{
					record.AddTo(table);
				}
			} while (hasRecord && table.Count < maximumCount);

			return table.Build().CreateRecords();
		}
	}
}