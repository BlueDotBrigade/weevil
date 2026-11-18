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
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;

	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using Cocona;
	using Cocona.Help;

	[TransformHelpFactory(typeof(InsightCommandHelp))]
	internal class InsightCommands
	{
		private readonly OutputOptions _output;

		public InsightCommands(OutputOptions output)
		{
			_output = output ?? throw new ArgumentNullException(nameof(output));
		}

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
			TelemetrySessionLifecycle telemetry = TelemetrySessionLifecycle.Shared;
			telemetry.StartSession("WeevilCli.exe", Program.ApplicationVersion, logPath);

			try
			{
				telemetry.Increment(TelemetryMetrics.CliInsightCommand);

				IEngine engine = Engine
						.UsingPath(logPath)
						.UsingTelemetry(telemetry)
						.UsingStorage(storage)
						.Open();

				// A log renamed, deleted or replaced meanwhile is not reloaded: the analyst runs the command again (#1247).
				// The report holds every insight and the metrics; the command shows what it always has (#837).
				InsightReport report = engine.RunOnce(
					() =>
					{
						ImmutableArray<IInsight> insights = engine.Analyzer.GetInsights();
						(DateTime From, DateTime To) period = engine.Records.GetEstimatedRange();
						return new InsightReportGenerator().Create(Program.ApplicationVersion, engine, insights, period.From, period.To);
					},
					Program.ReportWarning);

				if (_output.Format == OutputFormat.Json)
				{
					// The whole report, whatever --verbose says: a program filters the insights by their flag (#837).
					Write.Document(new JsonInsightReportRenderer().Render(report));
					return;
				}

				if (verbose)
				{
					Write.Heading("Metrics");

					Write.Text($"Total Records\t{report.RecordCount}");
					Write.Text($"File Size\t{report.FileSize}");

					foreach (InsightReportMetric metric in report.Metrics)
					{
						Write.Text($"{metric.Name}\t{metric.Value ?? "unknown"}");
					}
				}

				ImmutableArray<InsightReportItem> shown = verbose
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
			catch (Exception exception) when (exception is LogFileChangedException or NotSupportedException)
			{
				// A log changed while it was read (#1247), or one Weevil can't open, such as one with too many lines (#1252): its message, without a stack trace.
				// Cocona sets the exit code from the command, so it is passed this way.
				throw new CommandExitedException(exception.Message, 1);
			}
			finally
			{
				telemetry.EndSession();
			}
		}
	}
}
