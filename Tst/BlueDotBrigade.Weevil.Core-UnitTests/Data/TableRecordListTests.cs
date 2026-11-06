namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class TableRecordListTests
	{
		private static readonly DateTime Start = new DateTime(2026, 11, 6, 9, 0, 0);

		[TestMethod]
		public void GivenATable_WhenPositionsAreSelectedTwice_ThenEachListReadsTheRecordsAtItsOwnPositions()
		{
			var all = new TableRecordList(CreateTable());

			RecordList selected = TableRecordList.Select(all, new[] { 1, 2, 4 });
			RecordList selectedAgain = TableRecordList.Select(selected, new[] { 0, 2 });

			Describe(selected).Should().Equal("2 Two", "3 Gamma", "5 Five");
			Describe(selectedAgain).Should().Equal("2 Two", "5 Five");
			selectedAgain.GetLineNumber(1).Should().Be(5);
			selectedAgain.GetCreatedAt(1).Should().Be(Start.AddSeconds(4));
		}

		[TestMethod]
		public void GivenSelectedRecords_WhenSomeAreKept_ThenTheKeptListReadsTheSameRecords()
		{
			RecordList selected = TableRecordList.Select(new TableRecordList(CreateTable()), new[] { 0, 1, 2, 3 });

			// Bytes from the first chunk, a string, and bytes from the second chunk.
			RecordList kept = TableRecordList.Keep(selected, new[] { 0, 1, 3 });

			Describe(kept).Should().Equal("1 Alpha", "2 Two", "4 Delta");
			kept.GetCreatedAt(2).Should().Be(Start.AddSeconds(3));
		}

		[TestMethod]
		public void GivenNoPositions_WhenSelectedOrKept_ThenTheListIsEmpty()
		{
			var all = new TableRecordList(CreateTable());

			TableRecordList.Select(all, Array.Empty<int>()).Should().BeEmpty();
			TableRecordList.Keep(all, Array.Empty<int>()).Should().BeEmpty();
		}

		[TestMethod]
		public void GivenRecordsThatAreNotInATable_WhenSelectedOrKept_ThenThoseRecordsAreReturned()
		{
			RecordList records = R.WithLineNumbers(1, 5);

			TableRecordList.Select(records, new[] { 0, 4 }).Select(r => r.LineNumber).Should().Equal(1, 5);
			TableRecordList.Keep(records, new[] { 1, 3 }).Select(r => r.LineNumber).Should().Equal(2, 4);
		}

		/// <summary>
		/// Lines 1 to 5: Alpha and Gamma are bytes in one chunk, Delta is bytes in another, and Two and Five are strings.
		/// </summary>
		private static RecordTable CreateTable()
		{
			var first = new ContentChunk(0, Encoding.UTF8.GetBytes("Alpha\nGamma"), Encoding.UTF8);
			var second = new ContentChunk(12, Encoding.UTF8.GetBytes("Delta"), Encoding.UTF8);
			var facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);
			var builder = new RecordTable.Builder();

			builder.Add(1, Start, facts, first, 0, 5);
			builder.Add(2, Start.AddSeconds(1), facts, "Two");
			builder.Add(3, Start.AddSeconds(2), facts, first, 6, 5);
			builder.Add(4, Start.AddSeconds(3), facts, second, 0, 5);
			builder.Add(5, Start.AddSeconds(4), facts, "Five");

			return builder.Build();
		}

		private static string[] Describe(RecordList records)
		{
			return records.Select(r => $"{r.LineNumber} {r.Content}").ToArray();
		}
	}
}
