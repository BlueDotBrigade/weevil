namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Net;
	using System.Text;

	public sealed class HtmlInsightReportRenderer : IInsightReportRenderer
	{
		public string Render(InsightReport report)
		{
			if (report is null)
			{
				throw new ArgumentNullException(nameof(report));
			}

			var output = new StringBuilder();

			output.AppendLine($"<h1>{Encode(report.Title)}</h1>");
			output.AppendLine("<h2>General</h2>");
			output.AppendLine("<ul>");
			output.AppendLine($"<li>Context: {Encode(report.Context)}</li>");
			output.AppendLine($"<li>Time period analyzed: {Encode(report.From.ToString())} to {Encode(report.To.ToString())}</li>");
			output.AppendLine($"<li>Insight collected by: Weevil {Encode(report.WeevilVersion.ToString())}</li>");
			output.AppendLine("</ul>");
			output.AppendLine("<h2>Insight</h2>");
			output.AppendLine("<h3>Problem Areas</h3>");

			foreach (InsightReportItem item in report.ProblemAreas)
			{
				AppendInsight(output, item);
			}

			output.AppendLine("<h3>More Information</h3>");

			foreach (InsightReportItem item in report.MoreInformation)
			{
				AppendInsight(output, item);
			}

			return output.ToString();
		}

		private static void AppendInsight(StringBuilder output, InsightReportItem item)
		{
			output.AppendLine($"<h4>{Encode(item.Title)}</h4>");
			output.AppendLine("<ul>");
			output.AppendLine($"<li>Key Metric: {Encode(item.MetricValue)} {Encode(item.MetricUnit)}</li>");
			output.AppendLine($"<li>Details: {Encode(item.Details)}</li>");
			output.AppendLine("</ul>");
		}

		private static string Encode(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
	}
}
