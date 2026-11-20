namespace BlueDotBrigade.Weevil.PowerShell
{
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Analysis;
	using LogInUseException = BlueDotBrigade.Weevil.IO.LogInUseException;

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
				int insightCount;
				using (IEngine engine = Engine.UsingPath(logPath).Open())
				{
					insightCount = engine.Analyzer.GetInsights().Length;
				}

				var output = new GetInsightCmdlet { FilePath = logPath }.Invoke().Cast<object>().ToList();

				InsightReport report = output.Should().ContainSingle().Which.Should().BeOfType<InsightReport>().Subject;
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

		[TestMethod]
		[WorkItem(1254)]
		public void GivenALogAnotherWeevilHasOpen_WhenGettingInsights_ThenTheCmdletStopsWithAnErrorNamingWhoHasIt()
		{
			var folder = Directory.CreateTempSubdirectory("WeevilPowerShell.").FullName;

			try
			{
				var logPath = Path.Combine(folder, "Application.log");
				File.Copy(new Daten().AsFilePath("Application.log"), logPath);

				using (IEngine otherWeevil = Engine.UsingPath(logPath).Open())
				{
					var cmdlet = new GetInsightCmdlet { FilePath = logPath };

					// Invoked directly, a terminating error throws its exception.
					FluentActions.Invoking(() => cmdlet.Invoke().Cast<object>().ToList())
						.Should().Throw<LogInUseException>()
						.WithMessage($"Application.log is already open in Weevil by {Environment.UserName} on {Environment.MachineName}.");
				}
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}
	}
}
