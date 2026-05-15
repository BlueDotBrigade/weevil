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
		/// Sends telemetry asynchronously.
		/// </summary>
		/// <param name="session">
		/// Session payload to send.
		/// </param>
		/// <param name="ct">
		/// Cancellation token for cooperative cancellation.
		/// </param>
		Task SendAsync(TelemetrySession session, CancellationToken ct);

		/// <summary>
		/// Sends telemetry synchronously using best-effort semantics.
		/// </summary>
		/// <param name="session">
		/// Session payload to send.
		/// </param>
		void SendSync(TelemetrySession session);
	}
}
