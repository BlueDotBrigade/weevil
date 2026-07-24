namespace BlueDotBrigade.Weevil.Analysis
{
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

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			records[0].Metadata.IsFlagged.Should().BeTrue();
			records[1].Metadata.IsFlagged.Should().BeTrue();
			records[2].Metadata.IsFlagged.Should().BeFalse();
			records[0].Metadata.Comment.Should().Contain("Value: 2");
			records[1].Metadata.Comment.Should().Contain("Value: 2.5");
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

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			records[0].Metadata.IsFlagged.Should().BeTrue();
			records[1].Metadata.IsFlagged.Should().BeTrue();
			records[2].Metadata.IsFlagged.Should().BeFalse();
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

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			records[0].Metadata.IsFlagged.Should().BeTrue();
			records[1].Metadata.IsFlagged.Should().BeTrue();
			records[2].Metadata.IsFlagged.Should().BeFalse();
		}
	}
}
