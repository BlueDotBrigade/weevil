namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Text.Json;

	public sealed class JsonInsightReportRenderer : IInsightReportRenderer
	{
		public string Render(InsightReport report)
		{
			if (report is null)
			{
				throw new ArgumentNullException(nameof(report));
			}

			return JsonSerializer.Serialize(
				report,
				new JsonSerializerOptions
				{
					WriteIndented = true
				});
		}
	}
}
