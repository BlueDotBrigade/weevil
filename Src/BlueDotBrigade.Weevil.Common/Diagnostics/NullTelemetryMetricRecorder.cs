namespace BlueDotBrigade.Weevil.Diagnostics
{
	/// <summary>
	/// A no-op <see cref="ITelemetryMetricRecorder"/> used when no telemetry recorder has been configured.
	/// </summary>
	public sealed class NullTelemetryMetricRecorder : ITelemetryMetricRecorder
	{
		public static ITelemetryMetricRecorder Instance { get; } = new NullTelemetryMetricRecorder();

		private NullTelemetryMetricRecorder()
		{
		}

		public void Increment(string metricKey)
		{
			// no-op
		}
	}
}
