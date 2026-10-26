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

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = analyzer.Analyze(records, annotations, flags, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			flags.IsFlagged(records[1].LineNumber).Should().BeTrue();
			flags.IsFlagged(records[4].LineNumber).Should().BeTrue();
			annotations.GetComment(records[1].LineNumber).Should().Contain("01-Begins");
			annotations.GetComment(records[4].LineNumber).Should().Contain("01-Ends");
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

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = analyzer.Analyze(records, annotations, flags, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			flags.IsFlagged(records[1].LineNumber).Should().BeTrue();
			flags.IsFlagged(records[4].LineNumber).Should().BeTrue();
		}
	}
}
