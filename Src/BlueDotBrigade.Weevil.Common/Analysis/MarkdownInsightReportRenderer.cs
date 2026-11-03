namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Text;

	public sealed class MarkdownInsightReportRenderer : IInsightReportRenderer
	{
		public string Render(InsightReport report)
		{
			if (report is null)
			{
				throw new ArgumentNullException(nameof(report));
			}

			var output = new StringBuilder();

			output.AppendLine($"# {report.Title}");
			output.AppendLine();
			output.AppendLine("- [General](#general)");
			output.AppendLine("- [Insight](#insight)");
			output.AppendLine("   - [Problem Areas](#problem-areas)");
			output.AppendLine("   - [More Information](#more-information)");
			output.AppendLine();
			output.AppendLine("## General");
			output.AppendLine();
			output.AppendLine($" - Context: {report.Context}");
			output.AppendLine($" - Time period analyzed: {report.From} to {report.To}");
			output.AppendLine($" - Insight collected by: Weevil {report.WeevilVersion}");
			output.AppendLine();
			output.AppendLine("## Insight");
			output.AppendLine();
			output.AppendLine("### Problem Areas");
			output.AppendLine();

			foreach (InsightReportItem item in report.ProblemAreas)
			{
				AppendInsight(output, item);
			}

			output.AppendLine("### More Information");
			output.AppendLine();

			foreach (InsightReportItem item in report.MoreInformation)
			{
				AppendInsight(output, item);
			}

			return output.ToString();
		}

		private static void AppendInsight(StringBuilder output, InsightReportItem item)
		{
			output.AppendLine(item.Title);
			output.AppendLine();
			output.AppendLine($"- Key Metric: {item.MetricValue} {item.MetricUnit}");
			output.AppendLine($"- Details: {item.Details}");
			output.AppendLine();
		}
	}
}
