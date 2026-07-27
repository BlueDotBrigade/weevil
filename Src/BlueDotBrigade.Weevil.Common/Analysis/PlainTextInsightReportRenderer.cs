namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Text;

	public sealed class PlainTextInsightReportRenderer : IInsightReportRenderer
	{
		public string Render(InsightReport report)
		{
			if (report is null)
			{
				throw new ArgumentNullException(nameof(report));
			}

			var output = new StringBuilder();

			output.AppendLine(report.Title.ToUpperInvariant());
			output.AppendLine();
			output.AppendLine("GENERAL");
			output.AppendLine($"Context: {report.Context}");
			output.AppendLine($"Time period analyzed: {report.From} to {report.To}");
			output.AppendLine($"Insight collected by: Weevil {report.WeevilVersion}");
			output.AppendLine();
			output.AppendLine("INSIGHT");
			output.AppendLine("Problem Areas");

			foreach (InsightReportItem item in report.ProblemAreas)
			{
				AppendInsight(output, item);
			}

			output.AppendLine("More Information");

			foreach (InsightReportItem item in report.MoreInformation)
			{
				AppendInsight(output, item);
			}

			return output.ToString();
		}

		private static void AppendInsight(StringBuilder output, InsightReportItem item)
		{
			output.AppendLine(item.Title);
			output.AppendLine($"- Key Metric: {item.MetricValue} {item.MetricUnit}");
			output.AppendLine($"- Details: {item.Details}");
			output.AppendLine();
		}
	}
}
