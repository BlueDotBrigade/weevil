namespace BlueDotBrigade.Weevil.Telemetry.MsSql
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Diagnostics;
	using FluentAssertions;
	using Microsoft.Data.SqlClient;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class MsSqlTelemetryClientTests
	{
		private const string FakeConnectionString =
			"Server=localhost;Database=Weevil;User Id=telemetry;Password=x;";

		// ─── Constructor ───────────────────────────────────────────────────────────

		[TestMethod]
		public void GivenNullOptions_WhenConstructed_ThenThrowsArgumentNullException()
		{
			Action act = () => new MsSqlTelemetryClient(null);

			act.Should().Throw<ArgumentNullException>();
		}

		[TestMethod]
		public void GivenEmptyConnectionString_WhenConstructed_ThenDoesNotThrow()
		{
			var options = new MsSqlTelemetryClientOptions { ConnectionString = string.Empty };

			Action act = () => new MsSqlTelemetryClient(options);

			act.Should().NotThrow();
		}

		[TestMethod]
		public void GivenWhiteSpaceConnectionString_WhenConstructed_ThenDoesNotThrow()
		{
			var options = new MsSqlTelemetryClientOptions { ConnectionString = "   " };

			Action act = () => new MsSqlTelemetryClient(options);

			act.Should().NotThrow();
		}

		// ─── BuildSecuredConnectionString ──────────────────────────────────────────

		[TestMethod]
		public void GivenConnectionStringWithEncryptFalse_WhenBuildSecured_ThenEncryptIsMandatory()
		{
			var input = $"{FakeConnectionString}Encrypt=False;TrustServerCertificate=True;";

			var result = MsSqlTelemetryClient.BuildSecuredConnectionString(input);

			var builder = new SqlConnectionStringBuilder(result);
			builder.Encrypt.Should().Be(SqlConnectionEncryptOption.Mandatory);
		}

		[TestMethod]
		public void GivenConnectionStringWithTrustServerCertificateTrue_WhenBuildSecured_ThenTrustServerCertificateIsFalse()
		{
			var input = $"{FakeConnectionString}TrustServerCertificate=True;";

			var result = MsSqlTelemetryClient.BuildSecuredConnectionString(input);

			var builder = new SqlConnectionStringBuilder(result);
			builder.TrustServerCertificate.Should().BeFalse();
		}

		[TestMethod]
		public void GivenValidConnectionString_WhenBuildSecured_ThenEncryptIsMandatoryAndTrustServerCertificateIsFalse()
		{
			var result = MsSqlTelemetryClient.BuildSecuredConnectionString(FakeConnectionString);

			var builder = new SqlConnectionStringBuilder(result);
			builder.Encrypt.Should().Be(SqlConnectionEncryptOption.Mandatory);
			builder.TrustServerCertificate.Should().BeFalse();
		}

		// ─── SendAsync ─────────────────────────────────────────────────────────────

		[TestMethod]
		public async Task GivenNullSession_WhenSendAsyncCalled_ThenDoesNotThrow()
		{
			var client = CreateClientWithFakeConnection(commandTimeoutSeconds: 1);

			Func<Task> act = async () => await client.SendAsync(null, CancellationToken.None);

			await act.Should().NotThrowAsync();
		}

		[TestMethod]
		public async Task GivenAlreadyCancelledToken_WhenSendAsyncCalled_ThenDoesNotThrow()
		{
			var client = CreateClientWithFakeConnection(commandTimeoutSeconds: 1);
			var session = CreateMinimalSession();
			using var cts = new CancellationTokenSource();
			cts.Cancel();

			Func<Task> act = async () => await client.SendAsync(session, cts.Token);

			await act.Should().NotThrowAsync();
		}

		[TestMethod]
		public async Task GivenUnavailableDatabase_WhenSendAsyncCalled_ThenDoesNotThrow()
		{
			// Arrange: use a connection that will fail quickly.
			var client = CreateClientWithFakeConnection(commandTimeoutSeconds: 1);
			var session = CreateMinimalSession();

			// Act
			Func<Task> act = async () => await client.SendAsync(session, CancellationToken.None);

			// Assert: failure isolation — no exception may propagate.
			await act.Should().NotThrowAsync();
		}

		// ─── SendSync ──────────────────────────────────────────────────────────────

		[TestMethod]
		public void GivenNullSession_WhenSendSyncCalled_ThenDoesNotThrow()
		{
			var client = CreateClientWithFakeConnection(syncTimeoutSeconds: 1);

			Action act = () => client.SendSync(null);

			act.Should().NotThrow();
		}

		[TestMethod]
		public void GivenUnavailableDatabase_WhenSendSyncCalled_ThenDoesNotThrow()
		{
			// Arrange: use a short sync timeout so the test finishes quickly.
			var client = CreateClientWithFakeConnection(syncTimeoutSeconds: 1);
			var session = CreateMinimalSession();

			// Act
			Action act = () => client.SendSync(session);

			// Assert: failure isolation — no exception may propagate.
			act.Should().NotThrow();
		}

		// ─── Disabled (no credentials) ────────────────────────────────────────────

		[TestMethod]
		public void GivenEmptyConnectionString_WhenConstructed_ThenLogsWarning()
		{
			var capture = new CapturingLogWriter();
			var original = Log.Default;

			try
			{
				Log.Register(capture);
				var options = new MsSqlTelemetryClientOptions { ConnectionString = string.Empty };
				_ = new MsSqlTelemetryClient(options);

				capture.Warnings.Should().ContainSingle();
			}
			finally
			{
				Log.Register(original);
			}
		}

		[TestMethod]
		public async Task GivenEmptyConnectionString_WhenSendAsyncCalled_ThenDoesNotThrow()
		{
			var client = CreateDisabledClient();
			var session = CreateMinimalSession();

			Func<Task> act = async () => await client.SendAsync(session, CancellationToken.None);

			await act.Should().NotThrowAsync();
		}

		[TestMethod]
		public void GivenEmptyConnectionString_WhenSendSyncCalled_ThenDoesNotThrow()
		{
			var client = CreateDisabledClient();
			var session = CreateMinimalSession();

			Action act = () => client.SendSync(session);

			act.Should().NotThrow();
		}

		// ─── Helpers ───────────────────────────────────────────────────────────────

		private static MsSqlTelemetryClient CreateDisabledClient()
		{
			var options = new MsSqlTelemetryClientOptions { ConnectionString = string.Empty };
			return new MsSqlTelemetryClient(options);
		}

		private static MsSqlTelemetryClient CreateClientWithFakeConnection(
			int commandTimeoutSeconds = MsSqlTelemetryClientOptions.DefaultCommandTimeoutSeconds,
			int syncTimeoutSeconds = MsSqlTelemetryClientOptions.DefaultSyncTimeoutSeconds)
		{
			var options = new MsSqlTelemetryClientOptions
			{
				ConnectionString = FakeConnectionString,
				CommandTimeoutSeconds = commandTimeoutSeconds,
				SyncTimeoutSeconds = syncTimeoutSeconds,
			};

			return new MsSqlTelemetryClient(options);
		}

		private static TelemetrySession CreateMinimalSession()
		{
			return new TelemetrySession
			{
				SessionId = Guid.NewGuid(),
				Application = "WeevilGui.exe",
				Version = new Version(2, 12),
				SessionStartUtc = DateTime.UtcNow,
				SessionEndUtc = DateTime.UtcNow.AddMinutes(5),
				SessionActiveMinutes = 5,
				SchemaVersion = "1",
			};
		}

		private sealed class CapturingLogWriter : ILogWriter
		{
			public List<string> Warnings { get; } = new List<string>();

			public void Write(string message) { }
			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata) { }
			public void Write(LogSeverityType severity, Exception exception) { }
			public void Write(LogSeverityType severity, Exception exception, string message) { }
			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata) { }

			public void Write(LogSeverityType severity, string message)
			{
				if (severity == LogSeverityType.Warning)
				{
					Warnings.Add(message);
				}
			}

			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
				if (severity == LogSeverityType.Warning)
				{
					Warnings.Add(message);
				}
			}
		}
	}
}
