namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Text;

	[TestClass]
	public class RecordRangesTests
	{
		private static readonly RecordFacts Facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);

		[TestMethod]
		public void GivenATableOfSeveralChunksOnDisk_WhenSplit_ThenEachRangeStartsAtAChunksFirstRecord()
		{
			// A filter's worker reads a whole range, so it reads each chunk of a log on disk once (#1027).
			// Records kept as strings read no chunk, so the first range starts before any chunk does.
			var ranges = RecordRanges.Split(CreateTableOfTwoChunks(), alignToChunks: true, workerCount: 2);

			ranges.Should().Equal((0, 2), (2, 6), (6, 8));
		}

		[TestMethod]
		public void GivenATableInRam_WhenSplit_ThenItIsSplitIntoRangesOfAboutTheSameSize()
		{
			// In RAM, reading a chunk costs nothing, so a small log still keeps every core busy.
			// 4 ranges per worker.
			var ranges = RecordRanges.Split(CreateTableOfTwoChunks(), alignToChunks: false, workerCount: 1);

			ranges.Should().Equal((0, 2), (2, 4), (4, 6), (6, 8));
		}

		[TestMethod]
		public void GivenAtMostHalfAsManyChunksAsWorkersOnDisk_WhenSplit_ThenEachChunksRangeIsSplitSoMoreWorkersAreBusy()
		{
			// A small log on disk keeps more cores busy: each worker reads its chunk once. The table's 3 ranges each split in 2 for 6 workers.
			var ranges = RecordRanges.Split(CreateTableOfTwoChunks(), alignToChunks: true, workerCount: 6);

			ranges.Should().Equal((0, 1), (1, 2), (2, 4), (4, 6), (6, 7), (7, 8));
		}

		[TestMethod]
		public void GivenMoreThanHalfAsManyChunksAsWorkersOnDisk_WhenSplit_ThenEachRangeIsAWholeChunk()
		{
			// Splitting the table's 3 ranges in 2 for 5 workers would not finish sooner, and would read the log twice.
			var ranges = RecordRanges.Split(CreateTableOfTwoChunks(), alignToChunks: true, workerCount: 5);

			ranges.Should().Equal((0, 2), (2, 6), (6, 8));
		}

		[TestMethod]
		public void GivenATableWhoseRecordsReadNoChunk_WhenSplitAlongChunks_ThenItIsSplitIntoRangesOfAboutTheSameSize()
		{
			// As a log whose records all keep strings.
			var builder = new RecordTable.Builder();

			for (var lineNumber = 1; lineNumber <= 5; lineNumber++)
			{
				builder.Add(lineNumber, DateTime.MinValue, Facts, "Kept as a string");
			}

			var ranges = RecordRanges.Split(new TableRecordList(builder.Build()), alignToChunks: true, workerCount: 1);

			ranges.Should().Equal((0, 2), (2, 4), (4, 5));
		}

		private static TableRecordList CreateTableOfTwoChunks()
		{
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

			return new TableRecordList(builder.Build());
		}
	}
}
