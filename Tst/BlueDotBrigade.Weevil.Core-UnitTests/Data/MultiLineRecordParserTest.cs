namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

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
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			Assert.IsTrue(record.Content.StartsWith("2019-12-31 23:59:58"));
			Assert.IsTrue(record.Content.EndsWith("sapiente alienos esse arbitrantur."));
			Assert.AreEqual(258, record.Content.Length);
		}

		[TestMethod]
		public void GetNext_ReadingMultilineRecords_ReturnsLineNumberForStartOfEachRecord()
		{
			StreamReader dataSource = InputData.GetAsStreamReader("ReadUntilNextRecord.log");
			var multilineParser = new MultilineRecordParser(dataSource, new TsvRecordParser());

			IRecord multilineRecord = multilineParser.GetNext();
			IRecord singleLineRecord = multilineParser.GetNext();

			Assert.AreEqual(1, multilineRecord.LineNumber);
			Assert.AreEqual(4, singleLineRecord.LineNumber);
		}

		[TestMethod]
		public void GetNext_ReadUntilEof_ReturnsRecord()
		{
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			Assert.IsTrue(record.Content.StartsWith("2019-12-31 23:59:58"));
			Assert.IsTrue(record.Content.EndsWith("sapiente alienos esse arbitrantur."));
			Assert.AreEqual(258, record.Content.Length);
		}

		[TestMethod]
		public void GetNext_FileStartsWithPartialRecord_ReturnsRecord()
		{
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			Assert.IsTrue(record.Content.StartsWith("2019-12-31 23:59:58"));
			Assert.IsTrue(record.Content.EndsWith("sapiente alienos esse arbitrantur."));
			Assert.AreEqual(258, record.Content.Length);
		}

		[TestMethod]
		public void GetNext_ThreeOneLineRecords_ReturnsThreeRecords()
		{
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			var multilineParser = new MultilineRecordParser(dataSource, recordParser);

			Assert.IsTrue(multilineParser.GetNext().Content.EndsWith("First Record"));
			Assert.IsTrue(multilineParser.GetNext().Content.EndsWith("Second Record"));
			Assert.IsTrue(multilineParser.GetNext().Content.EndsWith("Third Record"));
		}

		[TestMethod]
		public void GetNext_NoRecords_ReturnsEmptyRecord()
		{
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			Assert.AreEqual(Record.Dummy, record);
		}

		[TestMethod]
		public void GetNext_RecordStartsAfterMaxLineCount_EmptyRecordReturned()
		{
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			// Won't be able to find anything
			// ... because the parser will give up before the first complete record is found
			IRecord record = new MultilineRecordParser(
				dataSource,
				recordParser,
				maximumLinesToSearch: 32,
				firstRecordLineNumber: 1,
				isLoggingEnabled: true).GetNext();

			Assert.AreEqual(33, CountNumberOfNewLines(dataSource));
			Assert.AreEqual(Record.Dummy, record);
		}

		[TestMethod]
		public void GetNext_RecordStartsBeforeMaxLineCount_RecordReturned()
		{
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			// Record is returned
			// ... because the parser will find it before it reaches the search limit
			IRecord record = new MultilineRecordParser(
				dataSource,
				recordParser,
				maximumLinesToSearch: 32,
				firstRecordLineNumber: 1,
				isLoggingEnabled: true).GetNext();

			Assert.AreEqual(12, CountNumberOfNewLines(dataSource));
			Assert.AreEqual(12, record.LineNumber);
		}

		[TestMethod]
		public void GetNext_RecordStartsDefaultMaxLinesWhileSearching_RecordReturned()
		{
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			Assert.AreEqual(256, MultilineRecordParser.MaximumLinesToSearch);
			Assert.AreEqual(MultilineRecordParser.MaximumLinesToSearch, record.LineNumber);
		}

		[TestMethod]
		public void GetNext_UnknownFileFormat_ReturnsEmptyRecord()
		{
			// We are intentionally reading in an image file
			// ... that cannot be interpreted as a log file
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			Assert.AreEqual(Record.Dummy, record);
		}

		[TestMethod]
		public void Metadata_IsMultilineRecord_HasMultilineAttribute()
		{
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			Assert.IsTrue(Convert.ToBoolean(record.LineNumber));
		}

		[TestMethod]
		public void Metadata_IsSingleLineRecord_DoesNotHaveMultilineAttribute()
		{
			StreamReader dataSource = InputData.GetAsStreamReader();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(dataSource, recordParser).GetNext();

			Assert.IsFalse(record.Metadata.IsMultiLine);
		}
	}
}