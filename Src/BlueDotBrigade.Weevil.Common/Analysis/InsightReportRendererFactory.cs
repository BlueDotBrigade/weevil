namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using BlueDotBrigade.Weevil.IO;

	public static class InsightReportRendererFactory
	{
		public static IInsightReportRenderer Create(IOutputFormatter formatter)
		{
			if (formatter is null)
			{
				throw new ArgumentNullException(nameof(formatter));
			}

			switch (formatter)
			{
				case MarkdownFormatter:
					return new MarkdownInsightReportRenderer();
				case PlainTextFormatter:
					return new PlainTextInsightReportRenderer();
				case HtmlFormatter:
					return new HtmlInsightReportRenderer();
				case JsonFormatter:
					return new JsonInsightReportRenderer();
				case XmlFormatter:
					return new XmlInsightReportRenderer();
				default:
					return new PlainTextInsightReportRenderer();
			}
		}
	}
}
