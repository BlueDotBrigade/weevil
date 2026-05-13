namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TelemetrySessionLifecycleTests
	{
		[TestMethod]
		public void GivenExistingSession_WhenNewFileIsOpened_ThenPreviousSessionIsEnded()
		{
			// Regression: Sub-task 3 (PR-3)
			var t0 = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
			var t1 = t0.AddSeconds(10);
			var times = new Queue<DateTime>(new[] { t0, t1 });
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));

			var firstPath = Path.GetTempFileName();
			var secondPath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(1, 2, 3), firstPath);
				var firstSessionId = tracker.CurrentSession.SessionId;

				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(1, 2, 3), secondPath);

				tracker.LastEndedSession.Should().NotBeNull();
				tracker.LastEndedSession!.SessionId.Should().Be(firstSessionId);
				tracker.LastEndedSession.SessionEndUtc.Should().Be(t1);
				tracker.CurrentSession.Should().NotBeNull();
				tracker.CurrentSession!.SessionId.Should().NotBe(firstSessionId);
			}
			finally
			{
				File.Delete(firstPath);
				File.Delete(secondPath);
			}
		}

		[TestMethod]
		public void GivenInstalledCpuProvided_WhenSessionStarts_ThenSessionContainsInstalledCpu()
		{
			var start = new DateTime(2026, 5, 1, 12, 30, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[] { start });
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen(
					"WeevilGui.exe",
					new Version(1, 0),
					sourcePath,
					installedRamMb: 8192,
					installedCpu: "AMD Ryzen 7");

				tracker.CurrentSession.Should().NotBeNull();
				tracker.CurrentSession!.InstalledCpu.Should().Be("AMD Ryzen 7");
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenInstalledCpuMissing_WhenSessionStarts_ThenSessionUsesUnknownCpu()
		{
			var start = new DateTime(2026, 5, 1, 12, 45, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[] { start });
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen(
					"WeevilGui.exe",
					new Version(1, 0),
					sourcePath,
					installedCpu: "   ");

				tracker.CurrentSession.Should().NotBeNull();
				tracker.CurrentSession!.InstalledCpu.Should().Be("");
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenStartupTelemetryContext_WhenSessionStarts_ThenSessionContainsSourceAndDebuggingState()
		{
			// Regression: Issue #803
			var start = new DateTime(2026, 5, 1, 11, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[] { start });
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));
			tracker.ConfigureStartupContext("ContosoInstaller", true);

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(1, 0), sourcePath);

				tracker.CurrentSession.Should().NotBeNull();
				tracker.CurrentSession!.Source.Should().Be("ContosoInstaller");
				tracker.CurrentSession.IsDebugging.Should().BeTrue();
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenNoStartupTelemetryContext_WhenSessionStarts_ThenSessionUsesSafeDefaults()
		{
			// Regression: Issue #803
			var start = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[] { start });
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(1, 0), sourcePath);

				tracker.CurrentSession.Should().NotBeNull();
				tracker.CurrentSession!.Source.Should().Be("unknown");
				tracker.CurrentSession.IsDebugging.Should().BeFalse();
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenIdlePeriodOverThreshold_WhenSessionEnds_ThenIdleTimeIsExcludedFromActiveMinutes()
		{
			// Regression: Sub-task 3 (PR-3)
			var start = new DateTime(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[]
			{
				start,
				start.AddSeconds(30),
				start.AddMinutes(2),
				start.AddMinutes(2.5),
			});
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen("WeevilCli.exe", new Version(2, 0), sourcePath);
				tracker.RecordSessionHeartbeat();
				tracker.RecordNavigationAction();
				var endedSession = tracker.EndCurrentSession();

				endedSession.Should().NotBeNull();
				endedSession!.SessionActiveMinutes.Should().BeApproximately(1.0, 0.0001);
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenFilterActivities_WhenRecorded_ThenFilterCountIsTracked()
		{
			// Regression: Sub-task 3 (PR-3)
			var start = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[]
			{
				start,
				start.AddSeconds(20),
				start.AddSeconds(40),
				start.AddSeconds(40),
			});
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(3, 1), sourcePath);
				tracker.RecordFilterExecution();
				tracker.RecordFilterExecution();
				var endedSession = tracker.EndCurrentSession();

				endedSession.Should().NotBeNull();
				endedSession!.FilterExecutionCount.Should().Be(2);
				endedSession.SessionActiveMinutes.Should().Be(0.667);
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public async Task GivenActiveSession_WhenNewFileOpened_ThenPreviousSessionSentAsync()
		{
			// Regression: Sub-task 4 (PR-4) - async upload on rollover
			var t0 = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
			var t1 = t0.AddSeconds(10);
			var times = new Queue<DateTime>(new[] { t0, t1 });
			var spyClient = new SpyTelemetryClient();
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));
			tracker.Configure(spyClient);

			var firstPath = Path.GetTempFileName();
			var secondPath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(1, 0), firstPath);
				var firstSessionId = tracker.CurrentSession.SessionId;

				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(1, 0), secondPath);

				// Allow the fire-and-forget async send to complete.
				await Task.Delay(200);

				spyClient.AsyncSentSessions.Should().ContainSingle()
					.Which.SessionId.Should().Be(firstSessionId);
			}
			finally
			{
				File.Delete(firstPath);
				File.Delete(secondPath);
			}
		}

		[TestMethod]
		public void GivenActiveSession_WhenEndCurrentSessionCalled_ThenSessionSentSync()
		{
			// Regression: Sub-task 4 (PR-4) - sync upload on shutdown/crash
			var start = new DateTime(2026, 4, 1, 13, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[] { start, start.AddSeconds(5) });
			var spyClient = new SpyTelemetryClient();
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));
			tracker.Configure(spyClient);

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen("WeevilCli.exe", new Version(2, 0), sourcePath);
				var sessionId = tracker.CurrentSession.SessionId;

				var endedSession = tracker.EndCurrentSession();

				spyClient.SyncSentSessions.Should().ContainSingle()
					.Which.SessionId.Should().Be(sessionId);
				endedSession.Should().NotBeNull();
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public async Task GivenRolloverThenShutdown_WhenBothOccur_ThenEachSessionSentExactlyOnce()
		{
			// Regression: Sub-task 4 (PR-4) - exactly-once enforcement
			var t0 = new DateTime(2026, 4, 1, 14, 0, 0, DateTimeKind.Utc);
			var t1 = t0.AddSeconds(10);
			var t2 = t1.AddSeconds(10);
			var times = new Queue<DateTime>(new[] { t0, t1, t2 });
			var spyClient = new SpyTelemetryClient();
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));
			tracker.Configure(spyClient);

			var firstPath = Path.GetTempFileName();
			var secondPath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(1, 0), firstPath);
				var firstSessionId = tracker.CurrentSession.SessionId;

				// Rollover: ends first session (async upload) and starts second session.
				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(1, 0), secondPath);
				var secondSessionId = tracker.CurrentSession.SessionId;

				// Shutdown: ends second session (sync upload).
				tracker.EndCurrentSession();

				// Allow the fire-and-forget async send of the first session to complete.
				await Task.Delay(200);

				spyClient.AsyncSentSessions.Should().ContainSingle()
					.Which.SessionId.Should().Be(firstSessionId);

				spyClient.SyncSentSessions.Should().ContainSingle()
					.Which.SessionId.Should().Be(secondSessionId);
			}
			finally
			{
				File.Delete(firstPath);
				File.Delete(secondPath);
			}
		}

		[TestMethod]
		public void GivenNoActiveSession_WhenEndCurrentSessionCalled_ThenNoUploadOccurs()
		{
			// Regression: Sub-task 4 (PR-4) - exactly-once / guard against no-op end
			var spyClient = new SpyTelemetryClient();
			var tracker = new TelemetrySessionLifecycle();
			tracker.Configure(spyClient);

			tracker.EndCurrentSession();

			spyClient.AsyncSentSessions.Should().BeEmpty();
			spyClient.SyncSentSessions.Should().BeEmpty();
		}

		[TestMethod]
		public void GivenNullClientConfigured_WhenSessionEnds_ThenFallsBackToNullClient()
		{
			// Regression: Sub-task 4 (PR-4) - null-safety for Configure
			var start = new DateTime(2026, 4, 1, 15, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[] { start, start.AddSeconds(5) });
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1));
			tracker.Configure(null);

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSessionOnFileOpen("WeevilGui.exe", new Version(1, 0), sourcePath);

				Action act = () => tracker.EndCurrentSession();

				act.Should().NotThrow();
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		private sealed class SpyTelemetryClient : ITelemetryClient
		{
			public List<TelemetrySession> AsyncSentSessions { get; } = new List<TelemetrySession>();
			public List<TelemetrySession> SyncSentSessions { get; } = new List<TelemetrySession>();

			public Task SendAsync(TelemetrySession session, CancellationToken ct)
			{
				if (session != null)
				{
					lock (AsyncSentSessions)
					{
						AsyncSentSessions.Add(session);
					}
				}

				return Task.CompletedTask;
			}

			public void SendSync(TelemetrySession session)
			{
				if (session != null)
				{
					SyncSentSessions.Add(session);
				}
			}
		}
	}
}
