namespace BlueDotBrigade.Weevil.Analysis
{
	using System;

	public sealed record InsightRelatedRecord
	{
		public required int LineNumber { get; init; }
		public DateTime? CreatedAt { get; init; }
		public required string Preview { get; init; }
	}
}
