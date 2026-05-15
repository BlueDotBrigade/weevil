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
		private readonly bool _isDisabled;

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
				_isDisabled = true;
				Log.Default.Write(LogSeverityType.Warning, "Telemetry has been disabled - no credentials were provided.");
			}
		}

		/// <inheritdoc/>
#pragma warning disable CA1031 // Intentional: telemetry failures must never propagate to the user workflow.
		public void Warmup()
		{
			if (_isDisabled)
			{
				return;
			}

			var connectionString = BuildSecuredConnectionString(
				_options.ConnectionString,
				_options.UsernameOrApiToken,
				_options.Secret,
				_options.ConnectionTimeoutSeconds);

			ThreadPool.QueueUserWorkItem(_ =>
			{
				try
				{
					Log.Default.Write(LogSeverityType.Information, "Telemetry warmup: connecting to database...");

					var stopwatch = Stopwatch.StartNew();

					using var connection = new SqlConnection(connectionString);
					connection.Open();

					stopwatch.Stop();

					Log.Default.Write(
						LogSeverityType.Information,
						$"Telemetry warmup succeeded. Connection time: {stopwatch.Elapsed.TotalSeconds:0.000}s.");
				}
				catch (Exception e)
				{
					Log.Default.Write(LogSeverityType.Error, e, "Telemetry warmup failed.");
				}
			});
		}
#pragma warning restore CA1031

		/// <inheritdoc/>
#pragma warning disable CA1031 // Intentional: telemetry failures must never propagate to the user workflow.
		public async Task SendAsync(TelemetrySession session, CancellationToken ct)
		{
			if (_isDisabled || session == null)
			{
				return;
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
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Error, e, "Failed to save telemetry session.");
			}
		}
#pragma warning restore CA1031

		/// <inheritdoc/>
#pragma warning disable CA1031 // Intentional: telemetry failures must never propagate to the user workflow.
		public void SendSync(TelemetrySession session)
		{
			if (_isDisabled || session == null)
			{
				return;
			}

			try
			{
				Log.Default.Write(LogSeverityType.Debug, "Saving telemetry session...");

				var stopwatch = Stopwatch.StartNew();

				using TelemetryDbContext context = CreateContext(_options.SyncTimeoutSeconds);
				context.Sessions.Add(session);
				context.SaveChanges();

				stopwatch.Stop();

				Log.Default.Write(
					LogSeverityType.Information,
					$"Telemetry session saved. Duration: {stopwatch.Elapsed.TotalSeconds:0.000}s.");
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Error, e, "Failed to save telemetry session.");
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
	}
}
