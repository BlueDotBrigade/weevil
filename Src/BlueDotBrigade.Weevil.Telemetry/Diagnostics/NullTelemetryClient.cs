namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// No-op telemetry client used when telemetry is disabled or unavailable.
	/// </summary>
	public sealed class NullTelemetryClient : ITelemetryClient
	{
		public static ITelemetryClient Instance { get; } = new NullTelemetryClient();

		public Task<TelemetryUploadStatus> UploadAsync(TelemetrySession session, CancellationToken ct)
		{
			return Task.FromResult(TelemetryUploadStatus.Disabled);
		}
	}
}
