namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class DetectRepeatingRecordsAnalyzerTests
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

			var analyzer = new DetectRepeatingRecordsAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?:2|2\.5)");

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			records[1].Metadata.IsFlagged.Should().BeTrue();
			records[4].Metadata.IsFlagged.Should().BeTrue();
			records[1].Metadata.Comment.Should().Contain("01-Begins");
			records[4].Metadata.Comment.Should().Contain("01-Ends");
		}
	}
}
