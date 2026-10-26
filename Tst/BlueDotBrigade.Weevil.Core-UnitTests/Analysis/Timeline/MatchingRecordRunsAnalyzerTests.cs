namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class MatchingRecordRunsAnalyzerTests
	{
		[TestMethod]
		public void GivenIntegerAndDecimalRepeatedBlock_WhenAnalyzeRuns_ThenBlockEdgesAreFlagged()
		{
			var records = R.Create()
				.WithContent("Value=0")
				.WithContent("Value=2")
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=2.5")
				.WithContent("Value=8")
				.GetRecords();

			var analyzer = new MatchingRecordRunsAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?:2|2\.5)");

			var state = new RecordState();

			Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			state.IsFlagged(records[1]).Should().BeTrue();
			state.IsFlagged(records[4]).Should().BeTrue();
			state.GetComment(records[1]).Should().Contain("01-Begins");
			state.GetComment(records[4]).Should().Contain("01-Ends");
		}

		[TestMethod]
		[WorkItem(923)]
		public void GivenKeepAllRecords_WhenAnalyzeRunsWithRegex_ThenBlockEdgesAreFlagged()
		{
			// An analyzer ignored its regular expression unless the active filter was a regular expression.
			var records = R.Create()
				.WithContent("Value=0")
				.WithContent("Value=2")
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=2.5")
				.WithContent("Value=8")
				.GetRecords();

			var analyzer = new MatchingRecordRunsAnalyzer(FilterStrategy.KeepAllRecords);
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?:2|2\.5)");

			var state = new RecordState();

			Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			state.IsFlagged(records[1]).Should().BeTrue();
			state.IsFlagged(records[4]).Should().BeTrue();
		}
	}
}
