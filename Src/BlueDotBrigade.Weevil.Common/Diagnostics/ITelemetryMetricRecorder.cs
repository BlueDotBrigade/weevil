namespace BlueDotBrigade.Weevil.Diagnostics
{
	/// <summary>
	/// Records a usage metric against the current telemetry session.
	/// </summary>
	/// <remarks>
	/// This abstraction lets <c>Core</c> (and other producers) record semantic usage metrics without
	/// referencing the telemetry implementation. Hosts inject a real recorder; otherwise the no-op
	/// <see cref="NullTelemetryMetricRecorder"/> is used.
	/// </remarks>
	public interface ITelemetryMetricRecorder
	{
		/// <summary>
		/// Increments the count for the supplied metric in the current session.
		/// </summary>
		/// <param name="metricKey">The metric name (for example, <c>Filter.Applied</c>).</param>
		void Increment(string metricKey);
	}
}
