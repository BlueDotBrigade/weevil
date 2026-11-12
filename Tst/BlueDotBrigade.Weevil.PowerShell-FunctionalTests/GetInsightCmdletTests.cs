namespace BlueDotBrigade.Weevil.PowerShell
{
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Analysis;

	[TestClass]
	public class GetInsightCmdletTests
	{
		[TestMethod]
		public void GivenALog_WhenGettingInsights_ThenOneReportHoldsTheLogsMetricsAndEveryInsight()
		{
			// Get-Insight returns the same report as the CLI's JSON, as an object (#837).
			var folder = Directory.CreateTempSubdirectory("WeevilPowerShell.").FullName;

			try
			{
				// Weevil saves a sidecar beside the log, so the test opens its own copy.
				var logPath = Path.Combine(folder, "Application.log");
				File.Copy(new Daten().AsFilePath("Application.log"), logPath);
				var insightCount = Engine.UsingPath(logPath).Open().Analyzer.GetInsights().Length;

				var output = new GetInsightCmdlet { FilePath = logPath }.Invoke().Cast<object>().ToList();

				var report = output.Should().ContainSingle().Which.Should().BeOfType<InsightReport>().Subject;
				report.SourceFileName.Should().Be("Application.log");
				report.RecordCount.Should().Be(9);
				report.Metrics.Should().Contain(metric => metric.Name == "Errors");
				report.Insights.Should().HaveCount(insightCount);
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}
	}
}
