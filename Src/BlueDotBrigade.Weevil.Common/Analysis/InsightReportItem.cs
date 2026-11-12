namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;

	/// <summary>
	/// One insight of an <see cref="InsightReport"/> (#837).
	/// </summary>
	public sealed record InsightReportItem
	{
		public required string Title { get; init; }

		public required string MetricValue { get; init; }

		public required string MetricUnit { get; init; }

		public required string Details { get; init; }

		public required bool IsAttentionRequired { get; init; }

		/// <summary>
		/// The line numbers of the records the insight refers to, with the primary one first. Never their text.
		/// </summary>
		public ImmutableArray<int> RelatedLineNumbers { get; init; } = ImmutableArray<int>.Empty;
	}
}
