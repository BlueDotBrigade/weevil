namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Linq;
	using System.Xml.Linq;

	public sealed class XmlInsightReportRenderer : IInsightReportRenderer
	{
		public string Render(InsightReport report)
		{
			if (report is null)
			{
				throw new ArgumentNullException(nameof(report));
			}

			var document = new XDocument(
				new XElement("InsightReport",
					new XElement("Title", report.Title ?? string.Empty),
					new XElement("SourceFileName", report.SourceFileName ?? string.Empty),
					new XElement("Context", report.Context ?? string.Empty),
					new XElement("WeevilVersion", report.WeevilVersion?.ToString() ?? string.Empty),
					new XElement("From", report.From),
					new XElement("To", report.To),
					new XElement("ProblemAreas", report.ProblemAreas.Select(ToElement)),
					new XElement("MoreInformation", report.MoreInformation.Select(ToElement))));

			return document.ToString();
		}

		private static XElement ToElement(InsightReportItem item)
		{
			return new XElement("Insight",
				new XElement("Title", item.Title ?? string.Empty),
				new XElement("MetricValue", item.MetricValue ?? string.Empty),
				new XElement("MetricUnit", item.MetricUnit ?? string.Empty),
				new XElement("Details", item.Details ?? string.Empty),
				new XElement("IsAttentionRequired", item.IsAttentionRequired),
				new XElement("RelatedRecords",
					item.RelatedRecords.Select(record =>
						new XElement("Record",
							new XElement("LineNumber", record.LineNumber),
							record.CreatedAt.HasValue
								? new XElement("CreatedAt", record.CreatedAt.Value)
								: null,
							new XElement("Preview", record.Preview ?? string.Empty)))));
		}
	}
}
