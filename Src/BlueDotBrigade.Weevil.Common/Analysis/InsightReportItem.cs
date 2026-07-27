namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;

	public sealed record InsightReportItem
	{
		public required string Title { get; init; }
		public required string MetricValue { get; init; }
		public required string MetricUnit { get; init; }
		public required string Details { get; init; }
		public required bool IsAttentionRequired { get; init; }
		public ImmutableArray<InsightRelatedRecord> RelatedRecords { get; init; } = ImmutableArray<InsightRelatedRecord>.Empty;
	}
}
