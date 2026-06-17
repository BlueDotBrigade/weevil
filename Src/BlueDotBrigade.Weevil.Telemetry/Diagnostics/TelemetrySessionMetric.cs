namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;

	/// <summary>
	/// A single extensible usage counter belonging to a <see cref="TelemetrySession"/>.
	/// </summary>
	/// <remarks>
	/// Stored as one row per key in <c>dbo.telemetry_session_metric</c>, keyed by
	/// <c>(session_id, metric_key)</c>. New metrics require no database schema change.
	/// </remarks>
	public sealed class TelemetrySessionMetric
	{
		/// <summary>
		/// Identifier of the owning session.
		/// </summary>
		public Guid SessionId { get; set; }

		/// <summary>
		/// Metric name (for example, <c>Filter.Applied</c>).
		/// </summary>
		public string MetricKey { get; set; } = string.Empty;

		/// <summary>
		/// Number of times the metric occurred during the session.
		/// </summary>
		public int MetricCount { get; set; }

		/// <summary>
		/// Navigation back to the owning session.
		/// </summary>
		public TelemetrySession Session { get; set; }
	}
}
