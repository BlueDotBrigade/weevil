namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Collections.Generic;
	using System.Linq;
	using global::BlueDotBrigade.Weevil.Analysis;
	using global::BlueDotBrigade.Weevil.Data;
	using NSubstitute;

	[TestClass]
	public class InsightReportGeneratorTests
	{
		[TestMethod]
		public void GivenInsights_WhenAReportIsCreated_ThenItHoldsEveryInsightInOrderWithWhetherItNeedsAttention()
		{
			// A program reads the whole report and filters it by the flag; the human output shows what it always has (#837).
			InsightReport report = Create(MarkdownInsightReportRendererTests.CreateInsights());

			report.Insights.Select(insight => (insight.Title, insight.IsAttentionRequired))
				.Should().Equal(("Critical Errors", true), ("Warnings", false));
		}

		[TestMethod]
		public void GivenAnInsightWithRelatedRecords_WhenAReportIsCreated_ThenItKeepsTheirLineNumbers()
		{
			// Line numbers, never record text, so building a report never reads the log (#837).
			IInsight insight = MarkdownInsightReportRendererTests.CreateInsight("Critical Errors", "2", "Critical errors found", isAttentionRequired: true, 42, 7);

			InsightReport report = Create(ImmutableArray.Create(insight));

			report.Insights.Single().RelatedLineNumbers.Should().Equal(42, 7);
		}

		[TestMethod]
		public void GivenAnOpenLog_WhenAReportIsCreated_ThenItHoldsTheLogsSizeAndTheFiltersMetricsInOrder()
		{
			// The CLI's --verbose shows them, as it did before the report had a model (#837).
			InsightReport report = Create(ImmutableArray<IInsight>.Empty);

			report.RecordCount.Should().Be(13);
			report.FileSize.Should().Be(2048);
			report.Metrics.Select(metric => metric.Name).Should().Equal("Information", "Warnings", "Errors");
		}

		[TestMethod]
		public void GivenALogsContext_WhenAReportIsCreated_ThenItIsKeptAsNamesAndValues()
		{
			// A program reads the context's values, rather than parsing "Name=Value;" text (#837).
			IEngine engine = MarkdownInsightReportRendererTests.CreateEngine();
			engine.Context.Returns(new ContextDictionary { { "Application", "Droid" }, { "Version", "2.1" } });

			InsightReport report = new InsightReportGenerator().Create(new Version(3, 0, 1), engine, ImmutableArray<IInsight>.Empty, from: null, to: null);

			report.Context.Should().Equal(new Dictionary<string, string> { { "Application", "Droid" }, { "Version", "2.1" } });
		}

		[TestMethod]
		public void GivenAPeriodThatIsNotKnown_WhenAReportIsCreated_ThenItsTimesAreNull()
		{
			// An estimated range returns CreationTimeUnknown for a log without timestamps; a program gets null instead (#837).
			InsightReport report = new InsightReportGenerator().Create(
				new Version(3, 0, 1),
				MarkdownInsightReportRendererTests.CreateEngine(),
				ImmutableArray<IInsight>.Empty,
				Record.CreationTimeUnknown,
				Record.CreationTimeUnknown);

			report.From.Should().BeNull();
			report.To.Should().BeNull();
		}

		[TestMethod]
		public void GivenAMetricWhoseTimeIsNotKnown_WhenAReportIsCreated_ThenItsValueIsNull()
		{
			// A log with no time gap has no first gap; a program gets null rather than 9999-12-31T23:59:59 (#837).
			IEngine engine = MarkdownInsightReportRendererTests.CreateEngine();
			engine.Filter.GetMetrics().Returns(new Dictionary<string, object>
			{
				{ "Counter", 0 },
				{ "FirstOccurredAt", Record.CreationTimeUnknown },
			});

			InsightReport report = new InsightReportGenerator().Create(new Version(3, 0, 1), engine, ImmutableArray<IInsight>.Empty, from: null, to: null);

			report.Metrics.Select(metric => (metric.Name, metric.Value))
				.Should().Equal(("Counter", 0), ("FirstOccurredAt", (object)null));
		}

		private static InsightReport Create(ImmutableArray<IInsight> insights)
		{
			return new InsightReportGenerator().Create(
				new Version(3, 0, 1),
				MarkdownInsightReportRendererTests.CreateEngine(),
				insights,
				from: null,
				to: null);
		}
	}
}
