namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;

	public sealed record InsightReport
	{
		public required string Title { get; init; }
		public required string SourceFileName { get; init; }
		public required string Context { get; init; }
		public required Version WeevilVersion { get; init; }
		public required DateTime From { get; init; }
		public required DateTime To { get; init; }
		public required ImmutableArray<InsightReportItem> ProblemAreas { get; init; }
		public required ImmutableArray<InsightReportItem> MoreInformation { get; init; }
	}
}
