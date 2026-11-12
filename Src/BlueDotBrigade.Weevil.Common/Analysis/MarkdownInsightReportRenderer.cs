namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Linq;
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

			output.AppendLine($"# Weevil Insight: {report.SourceFileName}");
			output.AppendLine();
			output.AppendLine("- [General](#general)");
			output.AppendLine("- [Insight](#insight)");
			output.AppendLine("   - [Problem Areas](#problem-areas)");
			output.AppendLine("   - [More Information](#more-information)");
			output.AppendLine();
			output.AppendLine("## General");
			output.AppendLine();
			output.AppendLine($" - Context: {FormatContext(report)}");
			output.AppendLine($" - Time period analyzed: {FormatTime(report.From)} to {FormatTime(report.To)}");
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

		/// <summary>
		/// "Name=Value; Name2=Value2;", as the context has always been shown, or "Not specified".
		/// </summary>
		private static string FormatContext(InsightReport report)
		{
			return report.Context.Count == 0
				? "Not specified"
				: string.Join(" ", report.Context.Select(property => $"{property.Key}={property.Value};"));
		}

		private static string FormatTime(DateTime? time)
		{
			return time?.ToString() ?? "unknown";
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
