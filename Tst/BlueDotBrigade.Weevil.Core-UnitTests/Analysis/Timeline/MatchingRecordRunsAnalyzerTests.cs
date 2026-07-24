namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
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

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			records[1].Metadata.IsFlagged.Should().BeTrue();
			records[4].Metadata.IsFlagged.Should().BeTrue();
			records[1].Metadata.Comment.Should().Contain("01-Begins");
			records[4].Metadata.Comment.Should().Contain("01-Ends");
		}

		[TestMethod]
		public void GivenKeepAllRecords_WhenAnalyzeRunsWithRegex_ThenBlockEdgesAreFlagged()
		{
			// Regression: Issue #923
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

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			records[1].Metadata.IsFlagged.Should().BeTrue();
			records[4].Metadata.IsFlagged.Should().BeTrue();
		}
	}
}
