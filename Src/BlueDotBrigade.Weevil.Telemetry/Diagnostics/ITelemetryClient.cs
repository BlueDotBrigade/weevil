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
		/// Uploads telemetry asynchronously.
		/// </summary>
		/// <remarks>
		/// The backend (Azure SQL serverless) may be paused; the first attempt typically fails while it
		/// resumes. Callers are expected to retry after a short delay rather than pre-warming the database.
		/// </remarks>
		/// <param name="session">
		/// Session payload to upload.
		/// </param>
		/// <param name="ct">
		/// Cancellation token for cooperative cancellation.
		/// </param>
		Task<TelemetryUploadStatus> UploadAsync(TelemetrySession session, CancellationToken ct);
	}
}
