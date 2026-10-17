namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class SerializedRecordRepositoryTests
	{
		private const int PreambleLineCount = 300;

		/// <summary>
		/// Creates a TSV log whose first record follows more lines than the parser searches by default.
		/// </summary>
		private static MemoryStream CreateLogWithLongPreamble()
		{
			var preamble = Enumerable
				.Range(1, PreambleLineCount)
				.Select(lineNumber => $"Preamble line {lineNumber}");

			var records = Enumerable
				.Range(PreambleLineCount + 1, 3)
				.Select(lineNumber => $"2026-10-16 10:00:00.00000\t1\t2\tInformation\tRecord {lineNumber}");

			var content = string.Join("\n", preamble.Concat(records));

			return new MemoryStream(Encoding.UTF8.GetBytes(content));
		}

		[TestMethod]
		public void GivenFirstRecordBeyondDefaultSearchLimit_WhenGettingRecords_ThenNoRecordsAreReturned()
		{
			var repository = new SerializedRecordRepository(CreateLogWithLongPreamble(), new TsvRecordParser());

			var records = repository.Get(new Range(PreambleLineCount + 1, int.MaxValue), int.MaxValue);

			records.Should().BeEmpty();
		}

		[TestMethod]
		public void GivenFirstRecordBeyondDefaultSearchLimit_WhenSearchLimitReachesIt_ThenRecordsAreReturned()
		{
			var repository = new SerializedRecordRepository(
				CreateLogWithLongPreamble(),
				new TsvRecordParser(),
				PreambleLineCount + MultilineRecordParser.MaximumLinesToSearch);

			var records = repository.Get(new Range(PreambleLineCount + 1, int.MaxValue), int.MaxValue);

			records.Should().HaveCount(3);
			records[0].LineNumber.Should().Be(PreambleLineCount + 1);
			records[0].Content.Should().EndWith($"Record {PreambleLineCount + 1}");
		}

		// Issue #977: the byte order mark is only at the top of the file, so reading from an offset must still decode UTF-16.
		[TestMethod]
		[DataRow(false, DisplayName = "UTF-16 little-endian")]
		[DataRow(true, DisplayName = "UTF-16 big-endian")]
		public void GivenUtf16Log_WhenReadingFromLineOffset_ThenContentIsDecoded(bool isBigEndian)
		{
			var encoding = new UnicodeEncoding(isBigEndian, byteOrderMark: true);
			const string firstLine = "2026-10-16 10:00:00.00000\t1\t2\tInformation\tRecord 1\n";
			const string secondLine = "2026-10-16 10:00:01.00000\t1\t2\tInformation\tRecord 2 \u00e9\n";
			var source = new MemoryStream(encoding.GetPreamble().Concat(encoding.GetBytes(firstLine + secondLine)).ToArray());
			var secondLineOffset = encoding.GetPreamble().Length + encoding.GetByteCount(firstLine);

			var repository = new SerializedRecordRepository(source, new TsvRecordParser(), secondLineOffset, 2, true);
			var records = repository.Get(int.MaxValue);

			records.Should().HaveCount(1);
			records[0].LineNumber.Should().Be(2);
			records[0].Content.Should().Be(secondLine.TrimEnd('\n'));
		}
	}
}
