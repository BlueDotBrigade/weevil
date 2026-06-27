namespace BlueDotBrigade.Weevil.Data.SqlClient
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Diagnostics;
	using Microsoft.Data.SqlClient;
	using Microsoft.EntityFrameworkCore;

	/// <summary>
	/// Telemetry client that writes session data to Azure SQL using Entity Framework.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Security guarantees enforced by this class:
	/// <list type="bullet">
	///   <item><description><c>Encrypt=True</c> — transport is always encrypted via TLS.</description></item>
	///   <item><description><c>TrustServerCertificate=False</c> — the server certificate is always validated.</description></item>
	/// </list>
	/// These settings override whatever the caller provides in the connection string.
	/// </para>
	/// <para>
	/// Failure isolation: all exceptions from the database layer are swallowed so that
	/// a telemetry failure never affects the normal user workflow.
	/// </para>
	/// <para>
	/// The SQL user configured for this client requires only INSERT permission on
	/// <c>telemetry.Session</c>.
	/// </para>
	/// </remarks>
	public sealed class MsSqlTelemetryClient : ITelemetryClient
	{
		private readonly MsSqlTelemetryClientOptions _options;
		private int _isUploadDisabled;

		/// <summary>
		/// Initializes a new instance of <see cref="MsSqlTelemetryClient"/>.
		/// </summary>
		/// <param name="options">Connection and timeout options.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// When <see cref="MsSqlTelemetryClientOptions.ConnectionString"/> is empty or whitespace,
		/// telemetry is treated as disabled: a warning is logged and all send operations become no-ops.
		/// </remarks>
		public MsSqlTelemetryClient(MsSqlTelemetryClientOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));

			if (string.IsNullOrWhiteSpace(options.ConnectionString))
			{
				DisableUpload();
				Log.Default.Write(LogSeverityType.Warning, "Telemetry has been disabled - no connection string was provided.");
			}
		}

		/// <inheritdoc/>
#pragma warning disable CA1031 // Intentional: telemetry failures must never propagate to the user workflow.
		public async Task<TelemetryUploadStatus> UploadAsync(TelemetrySession session, CancellationToken ct)
		{
			if (IsUploadDisabled)
			{
				return TelemetryUploadStatus.Disabled;
			}

			if (session == null)
			{
				return TelemetryUploadStatus.Success;
			}

			try
			{
				Log.Default.Write(LogSeverityType.Debug, "Saving telemetry session...");

				var stopwatch = Stopwatch.StartNew();

				using TelemetryDbContext context = CreateContext(_options.CommandTimeoutSeconds);
				context.Sessions.Add(session);
				await context.SaveChangesAsync(ct).ConfigureAwait(false);

				stopwatch.Stop();

				Log.Default.Write(
					LogSeverityType.Information,
					$"Telemetry session saved. Duration: {stopwatch.Elapsed.TotalSeconds:0.000}s.");
				return TelemetryUploadStatus.Success;
			}
			catch (Exception e)
			{
				var uploadStatus = ClassifyUploadException(e);
				if (uploadStatus == TelemetryUploadStatus.InvalidCredentials)
				{
					DisableUpload();
				}

				Log.Default.Write(LogSeverityType.Error, e, $"Failed to save telemetry session. Status={uploadStatus}.");
				return uploadStatus;
			}
		}
#pragma warning restore CA1031

		private TelemetryDbContext CreateContext(int commandTimeoutSeconds)
		{
			var securedConnectionString = BuildSecuredConnectionString(
				_options.ConnectionString,
				_options.UsernameOrApiToken,
				_options.Secret,
				_options.ConnectionTimeoutSeconds);

			DbContextOptions<TelemetryDbContext> contextOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
				.UseSqlServer(securedConnectionString, sqlOptions =>
				{
					sqlOptions.CommandTimeout(commandTimeoutSeconds);
				})
				.Options;

			return new TelemetryDbContext(contextOptions);
		}

		/// <summary>
		/// Parses <paramref name="connectionString"/> and enforces <c>Encrypt=True</c>,
		/// <c>TrustServerCertificate=False</c>, and <c>Connect Timeout</c>, overriding
		/// any caller-supplied values.
		/// </summary>
		internal static string BuildSecuredConnectionString(
			string connectionString,
			string usernameOrApiToken = "",
			string secret = "",
			int connectTimeoutSeconds = MsSqlTelemetryClientOptions.DefaultConnectionTimeoutSeconds)
		{
			var builder = new SqlConnectionStringBuilder(connectionString)
			{
				Encrypt = SqlConnectionEncryptOption.Mandatory,
				TrustServerCertificate = false,
				ConnectTimeout = connectTimeoutSeconds,
			};

			if (!string.IsNullOrWhiteSpace(usernameOrApiToken))
			{
				builder.UserID = usernameOrApiToken;
			}

			if (!string.IsNullOrWhiteSpace(secret))
			{
				builder.Password = secret;
			}

			return builder.ConnectionString;
		}

		private bool IsUploadDisabled => Volatile.Read(ref _isUploadDisabled) == 1;

		private void DisableUpload()
		{
			Interlocked.Exchange(ref _isUploadDisabled, 1);
		}

		private static TelemetryUploadStatus ClassifyUploadException(Exception exception)
		{
			if (TryGetSqlException(exception, out var sqlException))
			{
				if (sqlException.Number == 2601 || sqlException.Number == 2627)
				{
					return TelemetryUploadStatus.DuplicateSession;
				}

				if (sqlException.Number == 18456)
				{
					return TelemetryUploadStatus.InvalidCredentials;
				}
			}

			return exception.Message.IndexOf("login failed", StringComparison.OrdinalIgnoreCase) >= 0
				? TelemetryUploadStatus.InvalidCredentials
				: TelemetryUploadStatus.Failed;
		}

		private static bool TryGetSqlException(Exception exception, out SqlException sqlException)
		{
			for (var current = exception; current != null; current = current.InnerException)
			{
				if (current is SqlException foundException)
				{
					sqlException = foundException;
					return true;
				}
			}

			sqlException = null;
			return false;
		}
	}
}