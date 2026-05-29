namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Defines a transport for sending telemetry session data.
	/// </summary>
	public interface ITelemetryClient
	{
		/// <summary>
		/// Warms up the connection to the telemetry backend asynchronously (fire-and-forget).
		/// Call once when a log file is opened to ensure the backend is ready before the session ends.
		/// </summary>
		void Warmup();

		/// <summary>
		/// Uploads telemetry asynchronously.
		/// </summary>
		/// <param name="session">
		/// Session payload to upload.
		/// </param>
		/// <param name="ct">
		/// Cancellation token for cooperative cancellation.
		/// </param>
		Task<TelemetryUploadStatus> UploadAsync(TelemetrySession session, CancellationToken ct);
	}
}
