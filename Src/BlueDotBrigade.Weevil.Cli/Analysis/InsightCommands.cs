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
		public void Insight(string logPath, bool verbose = false)
		{
			IEngine engine = Engine
					.UsingPath(logPath)
					.Open();

			var severityMetrics =  engine.Filter.GetMetrics();

			var insights = engine.Analyzer.GetInsights();

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
	}
}