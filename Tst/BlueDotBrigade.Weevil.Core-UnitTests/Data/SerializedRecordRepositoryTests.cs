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
	}
}
