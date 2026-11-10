namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Text;

	[TestClass]
	public class RecordRangesTests
	{
		private static readonly RecordFacts Facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);

		[TestMethod]
		public void GivenATableOfSeveralChunks_WhenSplit_ThenEachRangeStartsAtAChunksFirstRecord()
		{
			// A filter's worker reads a whole range, so it reads each chunk of a log on disk once (#1027).
			// Records kept as strings read no chunk, so the first range starts before any chunk does.
			var first = new ContentChunk(0, Encoding.UTF8.GetBytes("abc"), Encoding.UTF8);
			var second = new ContentChunk(3, Encoding.UTF8.GetBytes("de"), Encoding.UTF8);
			var builder = new RecordTable.Builder();
			builder.Add(1, DateTime.MinValue, Facts, "Kept as a string");
			builder.Add(2, DateTime.MinValue, Facts, "Kept as a string");
			builder.Add(3, DateTime.MinValue, Facts, first, 0, 1);
			builder.Add(4, DateTime.MinValue, Facts, first, 1, 1);
			builder.Add(5, DateTime.MinValue, Facts, first, 2, 1);
			builder.Add(6, DateTime.MinValue, Facts, "Kept as a string");
			builder.Add(7, DateTime.MinValue, Facts, second, 0, 1);
			builder.Add(8, DateTime.MinValue, Facts, second, 1, 1);

			var ranges = RecordRanges.Split(new TableRecordList(builder.Build()));

			ranges.Should().Equal((0, 2), (2, 6), (6, 8));
		}

		[TestMethod]
		public void GivenATableWhoseRecordsReadNoChunk_WhenSplit_ThenItIsSplitIntoRangesOfTheDefaultSize()
		{
			// As a UTF-32 log in RAM, whose records keep strings.
			var builder = new RecordTable.Builder();

			for (var lineNumber = 1; lineNumber <= RecordRanges.DefaultRangeSize + 5; lineNumber++)
			{
				builder.Add(lineNumber, DateTime.MinValue, Facts, "Kept as a string");
			}

			var ranges = RecordRanges.Split(new TableRecordList(builder.Build()));

			ranges.Should().Equal((0, RecordRanges.DefaultRangeSize), (RecordRanges.DefaultRangeSize, RecordRanges.DefaultRangeSize + 5));
		}
	}
}
