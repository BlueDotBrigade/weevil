namespace BlueDotBrigade.Weevil.Data.SqlClient
{
	/// <summary>
	/// Configuration options for <see cref="MsSqlTelemetryClient"/>.
	/// </summary>
	public sealed class MsSqlTelemetryClientOptions
	{
		/// <summary>
		/// Default command timeout in seconds for asynchronous sends.
		/// </summary>
		public const int DefaultCommandTimeoutSeconds = 15;

		/// <summary>
		/// Default command timeout in seconds for synchronous best-effort sends.
		/// </summary>
		public const int DefaultSyncTimeoutSeconds = 15;

		/// <summary>
		/// Default TCP connection timeout in seconds.
		/// A longer value allows time for Azure SQL serverless to resume from a paused state.
		/// </summary>
		public const int DefaultConnectionTimeoutSeconds = 60;

		/// <summary>
		/// Azure SQL connection string. <c>Encrypt=True</c>, <c>TrustServerCertificate=False</c>, and
		/// <c>Connect Timeout</c> are enforced by <see cref="MsSqlTelemetryClient"/> regardless of what
		/// is specified here.
		/// </summary>
		public string ConnectionString { get; set; } = string.Empty;

		/// <summary>
		/// Optional SQL username or API token supplied outside the connection string.
		/// </summary>
		public string UsernameOrApiToken { get; set; } = string.Empty;

		/// <summary>
		/// Optional SQL secret supplied outside the connection string.
		/// </summary>
		public string Secret { get; set; } = string.Empty;

		/// <summary>
		/// Command timeout in seconds applied to asynchronous send operations.
		/// </summary>
		public int CommandTimeoutSeconds { get; set; } = DefaultCommandTimeoutSeconds;

		/// <summary>
		/// Command timeout in seconds applied to synchronous best-effort send operations.
		/// </summary>
		public int SyncTimeoutSeconds { get; set; } = DefaultSyncTimeoutSeconds;

		/// <summary>
		/// TCP connection timeout in seconds applied when establishing a database connection.
		/// A longer value allows time for Azure SQL serverless to resume from a paused state.
		/// </summary>
		public int ConnectionTimeoutSeconds { get; set; } = DefaultConnectionTimeoutSeconds;
	}
}
