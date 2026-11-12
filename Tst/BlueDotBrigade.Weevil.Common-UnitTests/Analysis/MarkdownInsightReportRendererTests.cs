namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Globalization;
	using global::BlueDotBrigade.Weevil.Analysis;
	using global::BlueDotBrigade.Weevil.Data;
	using NSubstitute;

	[TestClass]
	public class MarkdownInsightReportRendererTests
	{
		private CultureInfo _culture = CultureInfo.CurrentCulture;

		[TestInitialize]
		public void Setup()
		{
			// The report writes its period with the current culture, as the dashboard's Copy always has.
			_culture = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		}

		[TestCleanup]
		public void Cleanup()
		{
			CultureInfo.CurrentCulture = _culture;
		}

		[TestMethod]
		public void GivenInsights_WhenRenderedAsMarkdown_ThenTheReportIsTheOneTheDashboardCopied()
		{
			// The dashboard's Copy builds the report as a model, then renders it, and must copy the same text as before (#837).
			IEngine engine = CreateEngine();
			var from = new DateTime(2026, 5, 6, 10, 0, 0);
			var to = new DateTime(2026, 5, 6, 11, 0, 0);

			var generator = new InsightReportGenerator();
			var markdown = new MarkdownInsightReportRenderer().Render(generator.Create(new Version(3, 0, 1), engine, CreateInsights(), from, to));

			// The text the dashboard copied before #837, which plugins still get from Generate.
#pragma warning disable CS0618 // Generate is kept for plugins.
			markdown.Should().Be(generator.Generate(new Version(3, 0, 1), engine, CreateInsights(), from, to));
#pragma warning restore CS0618
			markdown.Should().Be(string.Join(Environment.NewLine, new[]
			{
				"# Weevil Insight: Application.log",
				"",
				"- [General](#general)",
				"- [Insight](#insight)",
				"   - [Problem Areas](#problem-areas)",
				"   - [More Information](#more-information)",
				"",
				"## General",
				"",
				" - Context: Not specified",
				" - Time period analyzed: 05/06/2026 10:00:00 to 05/06/2026 11:00:00",
				" - Insight collected by: Weevil 3.0.1",
				"",
				"## Insight",
				"",
				"### Problem Areas",
				"",
				"Critical Errors",
				"",
				"- Key Metric: 3 Σ",
				"- Details: Critical errors found",
				"",
				"### More Information",
				"",
				"Warnings",
				"",
				"- Key Metric: 10 Σ",
				"- Details: Warning records detected",
				"",
				"",
			}));
		}

		internal static IEngine CreateEngine()
		{
			var engine = Substitute.For<IEngine>();
			engine.SourceFilePath.Returns("/var/log/Application.log");
			engine.Context.Returns(ContextDictionary.Empty);
			engine.Metrics.Returns(new LogFileMetrics(fileSize: 2048, count: 13, TimeSpan.Zero, TimeSpan.Zero));
			engine.Filter.GetMetrics().Returns(new Dictionary<string, object>
			{
				{ "Information", 9 },
				{ "Warnings", 3 },
				{ "Errors", 1 },
			});

			return engine;
		}

		internal static ImmutableArray<IInsight> CreateInsights()
		{
			return ImmutableArray.Create(
				CreateInsight("Critical Errors", "3", "Critical errors found", isAttentionRequired: true),
				CreateInsight("Warnings", "10", "Warning records detected", isAttentionRequired: false));
		}

		internal static IInsight CreateInsight(string title, string metricValue, string details, bool isAttentionRequired, params int[] relatedLineNumbers)
		{
			var insight = Substitute.For<IInsight>();
			insight.Title.Returns(title);
			insight.MetricValue.Returns(metricValue);
			insight.MetricUnit.Returns("Σ");
			insight.Details.Returns(details);
			insight.IsAttentionRequired.Returns(isAttentionRequired);
			insight.RelatedLineNumbers.Returns(relatedLineNumbers);

			return insight;
		}
	}
}
