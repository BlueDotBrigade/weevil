namespace BlueDotBrigade.Weevil.Diagnostics
{
	/// <summary>
	/// Represents a telemetry session pending upload from the local outbox.
	/// </summary>
	public sealed class PendingTelemetrySession
	{
		public string FilePath { get; init; } = string.Empty;

		public TelemetrySessionDto Session { get; init; } = new TelemetrySessionDto();
	}
}
