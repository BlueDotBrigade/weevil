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

			var markdown = new MarkdownInsightReportRenderer().Render(new InsightReportGenerator().Create(new Version(3, 0, 1), engine, CreateInsights(), from, to));

			// The text the dashboard copied before #837, line for line.
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

		[TestMethod]
		public void GivenALogWithContextAndNoTimestamps_WhenRenderedAsMarkdown_ThenItShowsTheContextAndAnUnknownPeriod()
		{
			// The context reads as it always has; an unknown period is "unknown", not 12/31/9999 23:59:59 (#837).
			IEngine engine = CreateEngine();
			engine.Context.Returns(new ContextDictionary { { "Application", "Droid" }, { "Version", "2.1" } });

			var markdown = new MarkdownInsightReportRenderer().Render(new InsightReportGenerator().Create(
				new Version(3, 0, 1),
				engine,
				CreateInsights(),
				Record.CreationTimeUnknown,
				Record.CreationTimeUnknown));

			markdown.Should().Contain(" - Context: Application=Droid; Version=2.1;" + Environment.NewLine);
			markdown.Should().Contain(" - Time period analyzed: unknown to unknown" + Environment.NewLine);
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
