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

				var severityMetrics =  engine.Filter.GetMetrics();

				// On disk, a log that changed while it was read is reloaded, and read once more (#1028).
				var insights = engine.RunReloadingOnChange(current => current.Analyzer.GetInsights());

				if (verbose)
				{
					Write.Heading("Metrics");

					Write.Text($"Total Records\t{engine.Metrics.RecordCount}");
					Write.Text($"File Size\t{engine.Metrics.FileSize}");

					foreach (var item in severityMetrics)
					{
						Write.Text($"{item.Key}\t{item.Value}");
					}
				}else{
				
					insights = insights.Where(x => x.IsAttentionRequired == true).ToImmutableArray();
				}

				Write.Heading("Insights");

				if (insights.Length == 0)
				{
					Write.Text("No noteworthy insight was found.");
				}
				else
				{
					foreach(var insight in insights)
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
