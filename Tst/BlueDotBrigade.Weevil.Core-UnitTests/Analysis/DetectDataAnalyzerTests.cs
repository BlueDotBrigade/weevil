namespace BlueDotBrigade.Weevil.Analysis
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class DetectDataAnalyzerTests
	{
		[TestMethod]
		public void GivenIntegerAndDecimalMatches_WhenAnalyzeRuns_ThenMatchingRecordsAreFlagged()
		{
			var records = R.Create()
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=Done")
				.GetRecords();

			var analyzer = new DetectDataAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			var state = new RecordState();

			Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			state.IsFlagged(records[0]).Should().BeTrue();
			state.IsFlagged(records[1]).Should().BeTrue();
			state.IsFlagged(records[2]).Should().BeFalse();
			state.GetComment(records[0]).Should().Contain("Value: 2");
			state.GetComment(records[1]).Should().Contain("Value: 2.5");
		}

		[TestMethod]
		public void GivenKeepAllRecords_WhenAnalyzeRunsWithNamedGroupRegex_ThenMatchingRecordsAreFlagged()
		{
			// Regression: Issue #923
			var records = R.Create()
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=Done")
				.GetRecords();

			var analyzer = new DetectDataAnalyzer(FilterStrategy.KeepAllRecords);
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			var state = new RecordState();

			Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			state.IsFlagged(records[0]).Should().BeTrue();
			state.IsFlagged(records[1]).Should().BeTrue();
			state.IsFlagged(records[2]).Should().BeFalse();
		}

		[TestMethod]
		public void GivenPlainTextFilter_WhenAnalyzeRunsWithNamedGroupRegex_ThenMatchingRecordsAreFlagged()
		{
			// Regression: Issue #923
			var records = R.Create()
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=Done")
				.GetRecords();

			var analyzer = new DetectDataAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy(FilterType.PlainText));
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			var state = new RecordState();

			Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			state.IsFlagged(records[0]).Should().BeTrue();
			state.IsFlagged(records[1]).Should().BeTrue();
			state.IsFlagged(records[2]).Should().BeFalse();
		}
	}
}
