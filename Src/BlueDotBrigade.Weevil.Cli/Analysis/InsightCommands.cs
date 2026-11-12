namespace BlueDotBrigade.Weevil.Cli.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;

	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Analysis;

	using BlueDotBrigade.Weevil.Cli.IO;
	using BlueDotBrigade.Weevil.Diagnostics;

	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using Cocona;
	using Cocona.Help;

	[TransformHelpFactory(typeof(InsightCommandHelp))]
	internal class InsightCommands
	{
		[Command(
			name: "Insight",
			Aliases = new[] { "i" },
			Description = "Extracts key performance indicators (KPIs), and relevant metrics.")]
		public void Insight(
			string logPath,
			bool verbose = false,
			[Option(Description = "Ram keeps the log in memory. Disk reads it back from the log, for a log larger than memory.")]
			StorageMode storage = StorageMode.Ram)
		{
			var telemetry = TelemetrySessionLifecycle.Shared;
			telemetry.StartSession("WeevilCli.exe", Program.ApplicationVersion, logPath);

			try
			{
				telemetry.Increment(TelemetryMetrics.CliInsightCommand);

				IEngine engine = Engine
						.UsingPath(logPath)
						.UsingTelemetry(telemetry)
						.UsingStorage(storage)
						.Open();

				// On disk, a log that changed while it was read is reloaded, and read once more (#1028).
				var insights = engine.RunReloadingOnChange(current => current.Analyzer.GetInsights());

				// After the insights, so that the report's metrics describe the same log, if it was reloaded.
				// The report holds every insight and the metrics; the command shows what it always has (#837).
				InsightReport report = new InsightReportGenerator().Create(Program.ApplicationVersion, engine, insights, from: null, to: null);

				if (verbose)
				{
					Write.Heading("Metrics");

					Write.Text($"Total Records\t{report.RecordCount}");
					Write.Text($"File Size\t{report.FileSize}");

					foreach (InsightReportMetric metric in report.Metrics)
					{
						Write.Text($"{metric.Name}\t{metric.Value}");
					}
				}

				var shown = verbose
					? report.Insights
					: report.Insights.Where(insight => insight.IsAttentionRequired).ToImmutableArray();

				Write.Heading("Insights");

				if (shown.Length == 0)
				{
					Write.Text("No noteworthy insight was found.");
				}
				else
				{
					foreach (InsightReportItem insight in shown)
					{
						Write.Text($"{insight.Title}\t{insight.MetricValue} {insight.MetricUnit}\t{insight.Details}");
					}
				}
			}
			finally
			{
				telemetry.EndSession();
			}
		}
	}
}
