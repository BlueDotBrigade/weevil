namespace BlueDotBrigade.Weevil
{
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// No-op telemetry client used when telemetry is disabled or unavailable.
	/// </summary>
	public sealed class NullTelemetryClient : ITelemetryClient
	{
		public static ITelemetryClient Instance { get; } = new NullTelemetryClient();

		public Task SendAsync(TelemetrySession session, CancellationToken ct)
		{
			return Task.CompletedTask;
		}

		public void SendSync(TelemetrySession session)
		{
			// no-op
		}
	}
}
