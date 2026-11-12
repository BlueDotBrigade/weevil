namespace BlueDotBrigade.Weevil.Analysis
{
	/// <summary>
	/// One of the filter's metrics in an <see cref="InsightReport"/>, such as the number of errors (#837).
	/// </summary>
	public sealed record InsightReportMetric
	{
		public required string Name { get; init; }

		/// <summary>
		/// The value, such as a count, a duration or a time.
		/// </summary>
		public required object Value { get; init; }
	}
}
