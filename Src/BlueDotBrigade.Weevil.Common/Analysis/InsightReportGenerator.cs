namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.IO;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;

	public class InsightReportGenerator
	{
		private static string GetTableOfContents()
		{
			var output = new StringBuilder();

			output.AppendLine($"- [General](#general)");
			output.AppendLine($"- [Insight](#insight)");
			output.AppendLine($"   - [Problem Areas](#problem-areas)");
			output.AppendLine($"   - [More Information](#more-information)");

			return output.ToString();
		}

		private static string ToMarkdown(IInsight insight)
		{
			var output = new StringBuilder();

			output.AppendLine($"{insight.Title}");
			output.AppendLine($"");
			output.AppendLine($"- Key Metric: {insight.MetricValue} {insight.MetricUnit}");
			output.AppendLine($"- Details: {insight.Details}");

			return output.ToString();
		}

		public string Generate(IEngine engine, ImmutableArray<IInsight> insights, DateTime from, DateTime to)
		{
			var output = new StringBuilder();

			var fileName = Path.GetFileName(engine.SourceFilePath);
			var context = engine.Context.Count == 0 ? "Not specified" : engine.Context.ToString();

			output.AppendLine($"# Weevil Insight: {fileName}");
			output.AppendLine($"");
			output.AppendLine(GetTableOfContents());
			output.AppendLine($"## General");
			output.AppendLine($"");
			output.AppendLine($" - Context: {context}");
			output.AppendLine($" - Time period analyzed: {from} to {to}");
			output.AppendLine($"");
			output.AppendLine($"## Insight");
			output.AppendLine($"");
			output.AppendLine($"### Problem Areas");
			output.AppendLine($"");
			foreach (IInsight insight in insights)
			{
				if (insight.IsAttentionRequired)
				{
					output.AppendLine(ToMarkdown(insight));
				}
			}
			output.AppendLine($"### More Information");
			output.AppendLine($"");
			foreach (IInsight insight in insights)
			{
				if (!insight.IsAttentionRequired)
				{
					output.AppendLine(ToMarkdown(insight));
				}
			}

			return output.ToString();
		}
	}
}
