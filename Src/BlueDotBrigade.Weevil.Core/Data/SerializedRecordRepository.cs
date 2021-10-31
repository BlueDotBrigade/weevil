namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
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
			: this(source, recordParser, FirstRecordByteOffset, FirstRecordLineNumber, LoggingEnabled)
		{
			// nothing to do
		}

		public SerializedRecordRepository(Stream source, IRecordParser recordParser, long firstRecordByteOffset, int firstRecordLineNumber, bool loggingEnabled)
		{
			_source = source;
			_firstRecordByteOffset = firstRecordByteOffset;
			_firstRecordLineNumber = firstRecordLineNumber;

			_source.Position = _firstRecordByteOffset;

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Preparing to read records from disk. {nameof(firstRecordByteOffset)}={firstRecordByteOffset}, {nameof(firstRecordLineNumber)}={firstRecordLineNumber}");

			_recordParser = new MultilineRecordParser(
				new StreamReader(_source),
				recordParser,
				MultilineRecordParser.MaximumLinesToSearch,
				firstRecordLineNumber,
				loggingEnabled);
		}

		public IRecord GetNext()
		{
			return _recordParser.GetNext();
		}

		public ImmutableArray<IRecord> GetAll()
		{
			return Get(int.MaxValue);
		}

		public ImmutableArray<IRecord> Get(int maximumCount)
		{
			return Get(new Range(0, int.MaxValue), maximumCount);
		}

		public ImmutableArray<IRecord> Get(Range range, int maximumCount)
		{
			var results = new List<IRecord>();

			_source.Position = _firstRecordByteOffset;

			IRecord record = Record.Dummy;

			do
			{
				record = _recordParser.GetNext();

				if (Record.IsGenuine(record))
				{
					if (range.Minimum <= record.LineNumber &&
						record.LineNumber <= range.Maximum)
					{
						results.Add(record);
					}
				}
			} while (Record.IsGenuine(record) && results.Count < maximumCount);

			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

			return ImmutableArray.Create(results.ToArray());
		}
	}
}