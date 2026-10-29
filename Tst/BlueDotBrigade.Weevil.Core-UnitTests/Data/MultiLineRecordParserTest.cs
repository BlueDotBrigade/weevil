namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using System.Text;
	using BlueDotBrigade.Weevil.IO;
	using NSubstitute;

	[TestClass]
	public class MultiLineRecordParserTest
	{
		/// <summary>
		///     Represents the line number for the first log entry.
		/// </summary>
		private const int LineOne = 1;

		private int CountNumberOfNewLines(Stream source)
		{
			var startPosition = source.Position;
			var count = 0;

			source.Seek(0, SeekOrigin.Begin);

			using (var reader = new StreamReader(source, leaveOpen: true))
			{
				while (!reader.EndOfStream)
				{
					reader.ReadLine();
					count++;
				}
			}

			source.Seek(startPosition, SeekOrigin.Begin);

			return count;
		}

		[TestMethod]
		public void GetNext_ReadUntilNextRecord_ReturnsRecord()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(new LogLineReader(dataSource), recordParser).GetNext();

			(record.Content.StartsWith("2019-12-31 23:59:58")).Should().BeTrue();
			(record.Content.EndsWith("sapiente alienos esse arbitrantur.")).Should().BeTrue();
			record.Content.Length.Should().Be(258);
		}

		[TestMethod]
		public void GetNext_ReadingMultilineRecords_ReturnsLineNumberForStartOfEachRecord()
		{
			Stream dataSource = new Daten().AsStream("ReadUntilNextRecord.log");
			var multilineParser = new MultilineRecordParser(new LogLineReader(dataSource), new TsvRecordParser());

			IRecord multilineRecord = multilineParser.GetNext();
			IRecord singleLineRecord = multilineParser.GetNext();

			multilineRecord.LineNumber.Should().Be(1);
			singleLineRecord.LineNumber.Should().Be(4);
		}

		[TestMethod]
		public void GetNext_ReadUntilEof_ReturnsRecord()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(new LogLineReader(dataSource), recordParser).GetNext();

			(record.Content.StartsWith("2019-12-31 23:59:58")).Should().BeTrue();
			(record.Content.EndsWith("sapiente alienos esse arbitrantur.")).Should().BeTrue();
			record.Content.Length.Should().Be(258);
		}

		[TestMethod]
		public void GetNext_FileStartsWithPartialRecord_ReturnsRecord()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(new LogLineReader(dataSource), recordParser).GetNext();

			(record.Content.StartsWith("2019-12-31 23:59:58")).Should().BeTrue();
			(record.Content.EndsWith("sapiente alienos esse arbitrantur.")).Should().BeTrue();
			record.Content.Length.Should().Be(258);
		}

		[TestMethod]
		public void GetNext_ThreeOneLineRecords_ReturnsThreeRecords()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			var multilineParser = new MultilineRecordParser(new LogLineReader(dataSource), recordParser);

			(multilineParser.GetNext().Content.EndsWith("First Record")).Should().BeTrue();
			(multilineParser.GetNext().Content.EndsWith("Second Record")).Should().BeTrue();
			(multilineParser.GetNext().Content.EndsWith("Third Record")).Should().BeTrue();
		}

		[TestMethod]
		public void GetNext_NoRecords_ReturnsEmptyRecord()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(new LogLineReader(dataSource), recordParser).GetNext();

			record.Should().Be(Record.Dummy);
		}

		[TestMethod]
		public void GetNext_RecordStartsAfterMaxLineCount_EmptyRecordReturned()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			// Won't be able to find anything
			// ... because the parser will give up before the first complete record is found
			IRecord record = new MultilineRecordParser(
				new LogLineReader(dataSource),
				recordParser,
				maximumLinesToSearch: 32,
				firstRecordLineNumber: 1,
				isLoggingEnabled: true).GetNext();

			(CountNumberOfNewLines(dataSource)).Should().Be(33);
			record.Should().Be(Record.Dummy);
		}

		[TestMethod]
		public void GetNext_RecordStartsBeforeMaxLineCount_RecordReturned()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			// Record is returned
			// ... because the parser will find it before it reaches the search limit
			IRecord record = new MultilineRecordParser(
				new LogLineReader(dataSource),
				recordParser,
				maximumLinesToSearch: 32,
				firstRecordLineNumber: 1,
				isLoggingEnabled: true).GetNext();

			(CountNumberOfNewLines(dataSource)).Should().Be(12);
			record.LineNumber.Should().Be(12);
		}

		[TestMethod]
		public void GetNext_RecordStartsDefaultMaxLinesWhileSearching_RecordReturned()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(new LogLineReader(dataSource), recordParser).GetNext();

			MultilineRecordParser.MaximumLinesToSearch.Should().Be(256);
			record.LineNumber.Should().Be(MultilineRecordParser.MaximumLinesToSearch);
		}

		[TestMethod]
		public void GetNext_UnknownFileFormat_ReturnsEmptyRecord()
		{
			// We are intentionally reading in an image file
			// ... that cannot be interpreted as a log file
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(new LogLineReader(dataSource), recordParser).GetNext();

			record.Should().Be(Record.Dummy);
		}

		[TestMethod]
		public void Metadata_IsMultilineRecord_HasMultilineAttribute()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(new LogLineReader(dataSource), recordParser).GetNext();

			record.IsMultiLine.Should().BeTrue();
		}

		[TestMethod]
		public void Metadata_IsSingleLineRecord_DoesNotHaveMultilineAttribute()
		{
			Stream dataSource = new Daten().AsStream();
			var recordParser = new TsvRecordParser();

			IRecord record = new MultilineRecordParser(new LogLineReader(dataSource), recordParser).GetNext();

			record.IsMultiLine.Should().BeFalse();
		}

		private const string FirstLine = "2026-10-16 10:00:00.00000\t123\t2\tInformation\tFirst record";
		private const string SecondLine = "2026-10-16 10:00:01.00000\t123\t2\tWarning\tSecond record";
		private const string ContinuationLine1 = "   at Weevil.Component.Method()";
		private const string ContinuationLine2 = "   at Weevil.Component.Caller()";

		private static MultilineRecordParser CreateParser(params string[] lines)
		{
			var content = string.Join("\n", lines);
			var dataSource = new MemoryStream(Encoding.UTF8.GetBytes(content));

			return new MultilineRecordParser(new LogLineReader(dataSource), new TsvRecordParser());
		}

		[TestMethod]
		public void GivenSingleLineRecords_WhenReading_ThenEachRecordIsItsLine()
		{
			var parser = CreateParser(FirstLine, SecondLine);

			IRecord first = parser.GetNext();
			IRecord second = parser.GetNext();

			first.Content.Should().Be(FirstLine);
			first.LineNumber.Should().Be(1);
			first.IsMultiLine.Should().BeFalse();
			second.Content.Should().Be(SecondLine);
			second.LineNumber.Should().Be(2);
			second.Severity.Should().Be(SeverityType.Warning);
			second.IsMultiLine.Should().BeFalse();
		}

		[TestMethod]
		public void GivenMultilineRecord_WhenReading_ThenContinuationLinesAreJoinedWithCrLf()
		{
			var parser = CreateParser(FirstLine, ContinuationLine1, ContinuationLine2, SecondLine);

			IRecord first = parser.GetNext();
			IRecord second = parser.GetNext();

			first.Content.Should().Be($"{FirstLine}\r\n{ContinuationLine1}\r\n{ContinuationLine2}");
			first.IsMultiLine.Should().BeTrue();
			second.LineNumber.Should().Be(4);
			second.IsMultiLine.Should().BeFalse();
		}

		[TestMethod]
		public void GivenMultilineRecordFromUiThread_WhenReading_ThenItIsStillFromTheUiThread()
		{
			const string UiThreadLine = "2026-10-16 10:00:00.00000\t123\t1\tError\tFirst record";
			var parser = CreateParser(UiThreadLine, ContinuationLine1, SecondLine);

			IRecord first = parser.GetNext();

			// The record is created again once its continuation lines are read, keeping the parser's facts (#1012).
			first.IsMultiLine.Should().BeTrue();
			first.WasGeneratedByUi.Should().BeTrue();
			first.Severity.Should().Be(SeverityType.Error);
		}

		[TestMethod]
		public void GivenMultilineRecordAtEndOfFile_WhenReading_ThenContinuationLinesAreKept()
		{
			var parser = CreateParser(FirstLine, SecondLine, ContinuationLine1);

			parser.GetNext();
			IRecord last = parser.GetNext();

			last.Content.Should().Be($"{SecondLine}\r\n{ContinuationLine1}");
			last.IsMultiLine.Should().BeTrue();
		}

		[TestMethod]
		public void GivenParserReturnsCustomRecordType_WhenReading_ThenRecordIsReturned()
		{
			var customRecord = Substitute.For<IRecord>();
			customRecord.LineNumber.Returns(1);
			customRecord.Content.Returns("Custom record");

			var recordParser = Substitute.For<IRecordParser>();
			recordParser
				.TryParse(Arg.Any<int>(), Arg.Any<string>(), out Arg.Any<IRecord>())
				.Returns(call =>
				{
					call[2] = customRecord;
					return true;
				});

			var dataSource = new MemoryStream(Encoding.UTF8.GetBytes("line 1\nline 2"));

			IRecord record = new MultilineRecordParser(new LogLineReader(dataSource), recordParser).GetNext();

			record.Should().BeOfType<Record>();
			record.Content.Should().Be("Custom record");
		}
	}
}