namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Text;

	/// <summary>
	/// Renders an <see cref="InsightReport"/> as Markdown, for people: the report the dashboard copies (#837).
	/// </summary>
	public sealed class MarkdownInsightReportRenderer
	{
		public string Render(InsightReport report)
		{
			ArgumentNullException.ThrowIfNull(report);

			var output = new StringBuilder();
			var context = string.IsNullOrEmpty(report.Context) ? "Not specified" : report.Context;

			output.AppendLine($"# {report.Title}");
			output.AppendLine();
			output.AppendLine("- [General](#general)");
			output.AppendLine("- [Insight](#insight)");
			output.AppendLine("   - [Problem Areas](#problem-areas)");
			output.AppendLine("   - [More Information](#more-information)");
			output.AppendLine();
			output.AppendLine("## General");
			output.AppendLine();
			output.AppendLine($" - Context: {context}");
			output.AppendLine($" - Time period analyzed: {report.From} to {report.To}");
			output.AppendLine($" - Insight collected by: Weevil {report.WeevilVersion}");
			output.AppendLine();
			output.AppendLine("## Insight");
			output.AppendLine();
			output.AppendLine("### Problem Areas");
			output.AppendLine();
			AppendInsights(output, report, isAttentionRequired: true);
			output.AppendLine("### More Information");
			output.AppendLine();
			AppendInsights(output, report, isAttentionRequired: false);

			return output.ToString();
		}

		private static void AppendInsights(StringBuilder output, InsightReport report, bool isAttentionRequired)
		{
			foreach (InsightReportItem insight in report.Insights)
			{
				if (insight.IsAttentionRequired == isAttentionRequired)
				{
					output.AppendLine(insight.Title);
					output.AppendLine();
					output.AppendLine($"- Key Metric: {insight.MetricValue} {insight.MetricUnit}");
					output.AppendLine($"- Details: {insight.Details}");
					output.AppendLine();
				}
			}
		}
	}
}
