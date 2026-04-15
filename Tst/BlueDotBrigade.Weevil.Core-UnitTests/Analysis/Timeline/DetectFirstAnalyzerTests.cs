namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class DetectFirstAnalyzerTests
	{
		[TestMethod]
		public void GivenIntegerAndDecimalValues_WhenAnalyzeRuns_ThenFirstOccurrenceOfEachValueIsFlagged()
		{
			var records = R.Create()
				.WithContent("Value=0")
				.WithContent("Value=2")
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=2.5")
				.GetRecords();

			var analyzer = new DetectFirstAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(3);
			records[0].Metadata.IsFlagged.Should().BeTrue();
			records[1].Metadata.IsFlagged.Should().BeTrue();
			records[2].Metadata.IsFlagged.Should().BeFalse();
			records[3].Metadata.IsFlagged.Should().BeTrue();
			records[4].Metadata.IsFlagged.Should().BeFalse();
		}
	}
}
