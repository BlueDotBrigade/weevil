namespace BlueDotBrigade.Weevil.Telemetry.MsSql
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
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

		/// <summary>
		/// Initializes a new instance of <see cref="MsSqlTelemetryClient"/>.
		/// </summary>
		/// <param name="options">Connection and timeout options.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <see cref="MsSqlTelemetryClientOptions.ConnectionString"/> is empty.</exception>
		public MsSqlTelemetryClient(MsSqlTelemetryClientOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));

			if (string.IsNullOrWhiteSpace(options.ConnectionString))
			{
				throw new ArgumentException(
					"Connection string must not be null or empty.",
					nameof(options));
			}
		}

		/// <inheritdoc/>
#pragma warning disable CA1031 // Intentional: telemetry failures must never propagate to the user workflow.
		public async Task SendAsync(TelemetrySession session, CancellationToken ct)
		{
			if (session == null)
			{
				return;
			}

			try
			{
				using TelemetryDbContext context = CreateContext(_options.CommandTimeoutSeconds);
				context.Sessions.Add(ToRecord(session));
				await context.SaveChangesAsync(ct).ConfigureAwait(false);
			}
			catch (Exception)
			{
				// Failure isolation: telemetry must never affect the user workflow.
			}
		}
#pragma warning restore CA1031

		/// <inheritdoc/>
#pragma warning disable CA1031 // Intentional: telemetry failures must never propagate to the user workflow.
		public void SendSync(TelemetrySession session)
		{
			if (session == null)
			{
				return;
			}

			try
			{
				using TelemetryDbContext context = CreateContext(_options.SyncTimeoutSeconds);
				context.Sessions.Add(ToRecord(session));
				context.SaveChanges();
			}
			catch (Exception)
			{
				// Failure isolation: telemetry must never affect the user workflow.
			}
		}
#pragma warning restore CA1031

		private TelemetryDbContext CreateContext(int commandTimeoutSeconds)
		{
			var securedConnectionString = BuildSecuredConnectionString(_options.ConnectionString);

			DbContextOptions<TelemetryDbContext> contextOptions = new DbContextOptionsBuilder<TelemetryDbContext>()
				.UseSqlServer(securedConnectionString, sqlOptions =>
				{
					sqlOptions.CommandTimeout(commandTimeoutSeconds);
				})
				.Options;

			return new TelemetryDbContext(contextOptions);
		}

		/// <summary>
		/// Parses <paramref name="connectionString"/> and enforces <c>Encrypt=True</c> and
		/// <c>TrustServerCertificate=False</c>, overriding any caller-supplied values.
		/// </summary>
		internal static string BuildSecuredConnectionString(string connectionString)
		{
			var builder = new SqlConnectionStringBuilder(connectionString)
			{
				Encrypt = SqlConnectionEncryptOption.Mandatory,
				TrustServerCertificate = false,
			};

			return builder.ConnectionString;
		}

		private static SessionRecord ToRecord(TelemetrySession session)
		{
			return new SessionRecord
			{
				SessionId = session.SessionId,
				Application = session.Application,
				Version = session.Version?.ToString() ?? string.Empty,
				SessionStartUtc = session.SessionStartUtc,
				SessionEndUtc = session.SessionEndUtc,
				SessionActiveMinutes = session.SessionActiveMinutes,
				LogFileSizeBytes = session.LogFileSizeBytes,
				InstalledRamMb = session.InstalledRamMb,
				FilterExecutionCount = session.FilterExecutionCount,
				GraphOpenCount = session.GraphOpenCount,
				DashboardOpenCount = session.DashboardOpenCount,
				SchemaVersion = session.SchemaVersion,
			};
		}
	}
}
