namespace BlueDotBrigade.Weevil.Cli.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Linq;
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
		public void Insight(string logPath, bool verbose = false)
		{
			var telemetry = TelemetrySessionLifecycle.Shared;
			telemetry.StartSession("WeevilCli.exe", Program.ApplicationVersion, logPath);

			try
			{
				telemetry.Increment(TelemetryMetrics.CliInsightCommand);

				IEngine engine = Engine
						.UsingPath(logPath)
						.UsingTelemetry(telemetry)
						.Open();

				var insights = engine.Analyzer.GetInsights();

				if (!verbose)
				{
					insights = insights.Where(x => x.IsAttentionRequired).ToImmutableArray();
				}

				var range = engine.Filter.Results.GetEstimatedRange();
				var report = new InsightReportGenerator().Generate(
					Program.ApplicationVersion,
					engine,
					insights,
					range.From,
					range.To);
				var formatter = OutputAs.ResolveFormatter(
					Environment.GetCommandLineArgs().Skip(1).ToArray(),
					new MarkdownFormatter());
				var renderer = InsightReportRendererFactory.Create(formatter);

				Console.Out.WriteLine(renderer.Render(report));
			}
			finally
			{
				telemetry.EndSession();
			}
		}
	}
}
