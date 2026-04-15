namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class StableValueAnalyzerTests
	{
		[TestMethod]
		public void GivenIntegerAndDecimalStableRuns_WhenAnalyzeRuns_ThenRunBoundariesAreFlagged()
		{
			var records = R.Create()
				.WithContent("Value=2")
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=2.5")
				.GetRecords();

			var analyzer = new StableValueAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(4);
			records[0].Metadata.Comment.Should().Contain("Start Value: 2");
			records[1].Metadata.Comment.Should().Contain("Stop Value: 2");
			records[2].Metadata.Comment.Should().Contain("Start Value: 2.5");
			records[3].Metadata.Comment.Should().Contain("Stop Value: 2.5");
		}
	}
}
