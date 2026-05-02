namespace BlueDotBrigade.Weevil.Analysis
{
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class StatisticalAnalyzerTests
	{
		[TestMethod]
		public void GivenIntegerAndDecimalValues_WhenAnalyzeRuns_ThenStatisticsAreCalculated()
		{
			var records = R.Create()
				.WithContent("Value=0")
				.WithContent("Value=2")
				.WithContent("Value=4.5")
				.GetRecords();

			var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(3);
			results.Data.Should().ContainKey("Mean");
			results.Data["Mean"].Should().Be(2.167d);
			results.Data["Count"].Should().Be(3d);
		}
	}
}
