namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Text;
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
				CreateReader(_source, _firstRecordByteOffset),
				recordParser,
				maximumLinesToSearch,
				firstRecordLineNumber,
				loggingEnabled);
		}

		/// <summary>
		/// Creates a reader positioned at the first record.
		/// </summary>
		/// <remarks>
		/// The encoding is detected from a byte order mark, which is only at the top of the file.
		/// So when reading starts further in, the encoding is detected at the top first.
		/// </remarks>
		private static StreamReader CreateReader(Stream source, long firstRecordByteOffset)
		{
			source.Position = 0;

			if (firstRecordByteOffset == FirstRecordByteOffset)
			{
				return new StreamReader(source);
			}

			Encoding encoding;

			using (var detector = new StreamReader(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4, leaveOpen: true))
			{
				detector.Peek();
				encoding = detector.CurrentEncoding;
			}

			source.Position = firstRecordByteOffset;

			return new StreamReader(source, WithoutPreamble(encoding), detectEncodingFromByteOrderMarks: false);
		}

		/// <summary>
		/// Returns the same encoding without a byte order mark, so that the reader does not skip one at the first record.
		/// </summary>
		private static Encoding WithoutPreamble(Encoding encoding)
		{
			return encoding switch
			{
				UnicodeEncoding => new UnicodeEncoding(bigEndian: encoding.CodePage == Encoding.BigEndianUnicode.CodePage, byteOrderMark: false),
				UTF32Encoding => new UTF32Encoding(bigEndian: encoding.CodePage != Encoding.UTF32.CodePage, byteOrderMark: false),
				_ => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
			};
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
			return Get(Range.All, maximumCount);
		}

		public ImmutableArray<IRecord> Get(Range range, int maximumCount)
		{
			var results = new List<IRecord>();

			_source.Position = _firstRecordByteOffset;

			IRecord record = Record.Dummy;

			if (range.Equals(Range.All))
			{
				range = new Range(1, int.MaxValue);
			}

			do
			{
				record = _recordParser.GetNext();

				if (Record.IsGenuine(record))
				{
					if (range.Start.Value <= record.LineNumber &&
					    record.LineNumber <= range.End.Value)
					{
						results.Add(record);
					}
				}
			} while (Record.IsGenuine(record) && results.Count < maximumCount);

			return results.ToImmutableArray();
		}
	}
}