namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;

	[TestClass]
	public class MultiLineRecordParserTest
	{
		/// <summary>
		///     Represents the line number for the first log entry.
		/// </summary>
		private const int LineOne = 1;

		private int CountNumberOfNewLines(StreamReader reader)
		{
			var startPosition = reader.BaseStream.Position;
			var count = 0;

			reader.BaseStream.Seek(0, SeekOrigin.Begin);

			while (!reader.EndOfStream)
			{
				reader.ReadLine();
				count++;
			}

			reader.BaseStream.Seek(startPosition, SeekOrigin.Begin);

			return count;
		}

		[TestMethod]
		public void GetNext_ReadUntilNextRecord_ReturnsRecord()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			(record.Content.StartsWith("2019-12-31 23:59:58")).Should().BeTrue();
			(record.Content.EndsWith("sapiente alienos esse arbitrantur.")).Should().BeTrue();
			(record.Content.Length).Should().Be(258);
		}

		[TestMethod]
		public void GetNext_ReadingMultilineRecords_ReturnsLineNumberForStartOfEachRecord()
		{
			StreamReader dataSource = new Daten().AsStreamReader("ReadUntilNextRecord.log");
			var multilineParser = new MultilineRecordParser(dataSource, new TsvRecordParser());

			IRecord multilineRecord = multilineParser.GetNext();
			IRecord singleLineRecord = multilineParser.GetNext();

			(multilineRecord.LineNumber).Should().Be(1);
			(singleLineRecord.LineNumber).Should().Be(4);
		}

		[TestMethod]
		public void GetNext_ReadUntilEof_ReturnsRecord()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			(record.Content.StartsWith("2019-12-31 23:59:58")).Should().BeTrue();
			(record.Content.EndsWith("sapiente alienos esse arbitrantur.")).Should().BeTrue();
			(record.Content.Length).Should().Be(258);
		}

		[TestMethod]
		public void GetNext_FileStartsWithPartialRecord_ReturnsRecord()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			(record.Content.StartsWith("2019-12-31 23:59:58")).Should().BeTrue();
			(record.Content.EndsWith("sapiente alienos esse arbitrantur.")).Should().BeTrue();
			(record.Content.Length).Should().Be(258);
		}

		[TestMethod]
		public void GetNext_ThreeOneLineRecords_ReturnsThreeRecords()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			var multilineParser = new MultilineRecordParser(dataSource, recordParser);

			(multilineParser.GetNext().Content.EndsWith("First Record")).Should().BeTrue();
			(multilineParser.GetNext().Content.EndsWith("Second Record")).Should().BeTrue();
			(multilineParser.GetNext().Content.EndsWith("Third Record")).Should().BeTrue();
		}

		[TestMethod]
		public void GetNext_NoRecords_ReturnsEmptyRecord()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			(record).Should().Be(Record.Dummy);
		}

		[TestMethod]
		public void GetNext_RecordStartsAfterMaxLineCount_EmptyRecordReturned()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			// Won't be able to find anything
			// ... because the parser will give up before the first complete record is found
			IRecord record = new MultilineRecordParser(
				dataSource,
				recordParser,
				maximumLinesToSearch: 32,
				firstRecordLineNumber: 1,
				isLoggingEnabled: true).GetNext();

			(CountNumberOfNewLines(dataSource)).Should().Be(33);
			(record).Should().Be(Record.Dummy);
		}

		[TestMethod]
		public void GetNext_RecordStartsBeforeMaxLineCount_RecordReturned()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			// Record is returned
			// ... because the parser will find it before it reaches the search limit
			IRecord record = new MultilineRecordParser(
				dataSource,
				recordParser,
				maximumLinesToSearch: 32,
				firstRecordLineNumber: 1,
				isLoggingEnabled: true).GetNext();

			(CountNumberOfNewLines(dataSource)).Should().Be(12);
			(record.LineNumber).Should().Be(12);
		}

		[TestMethod]
		public void GetNext_RecordStartsDefaultMaxLinesWhileSearching_RecordReturned()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			(MultilineRecordParser.MaximumLinesToSearch).Should().Be(256);
			(record.LineNumber).Should().Be(MultilineRecordParser.MaximumLinesToSearch);
		}

		[TestMethod]
		public void GetNext_UnknownFileFormat_ReturnsEmptyRecord()
		{
			// We are intentionally reading in an image file
			// ... that cannot be interpreted as a log file
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			(record).Should().Be(Record.Dummy);
		}

		[TestMethod]
		public void Metadata_IsMultilineRecord_HasMultilineAttribute()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			(Convert.ToBoolean(record.LineNumber)).Should().BeTrue();
		}

		[TestMethod]
		public void Metadata_IsSingleLineRecord_DoesNotHaveMultilineAttribute()
		{
			StreamReader dataSource = new Daten().AsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			(record.Metadata.IsMultiLine).Should().BeFalse();
		}
	}
}