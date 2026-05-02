namespace BlueDotBrigade.Weevil.Analysis
{
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
	}
}
