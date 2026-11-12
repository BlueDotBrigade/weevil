namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;
	using global::BlueDotBrigade.Weevil.Analysis;

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
			report.Context.Should().BeEmpty("the log has no context");
			report.From.Should().BeNull("the period is not known");
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
