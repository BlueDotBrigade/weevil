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
		public const int DefaultCommandTimeoutSeconds = 30;

		/// <summary>
		/// Default command timeout in seconds for synchronous best-effort sends.
		/// </summary>
		public const int DefaultSyncTimeoutSeconds = 5;

		/// <summary>
		/// Azure SQL connection string. <c>Encrypt=True</c> and <c>TrustServerCertificate=False</c>
		/// are enforced by <see cref="MsSqlTelemetryClient"/> regardless of what is specified here.
		/// </summary>
		public string ConnectionString { get; set; } = string.Empty;

		/// <summary>
		/// Command timeout in seconds applied to asynchronous send operations.
		/// </summary>
		public int CommandTimeoutSeconds { get; set; } = DefaultCommandTimeoutSeconds;

		/// <summary>
		/// Command timeout in seconds applied to synchronous best-effort send operations.
		/// </summary>
		public int SyncTimeoutSeconds { get; set; } = DefaultSyncTimeoutSeconds;
	}
}
