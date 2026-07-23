namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
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
				tracker.StartSession("WeevilGui.exe", new Version(1, 2, 3), firstPath);
				var firstSessionId = tracker.CurrentSession.SessionId;

				tracker.StartSession("WeevilGui.exe", new Version(1, 2, 3), secondPath);

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
		public void GivenHelpActivities_WhenRecorded_ThenHelpOpenCountIsTracked()
		{
			var start = new DateTime(2026, 6, 3, 9, 0, 0, DateTimeKind.Utc);
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
				tracker.StartSession("WeevilGui.exe", new Version(3, 1), sourcePath);
				tracker.RecordHelpOpen();
				tracker.RecordHelpOpen();
				var endedSession = tracker.EndSession();

				endedSession.Should().NotBeNull();
				endedSession!.Metrics.Single(m => m.MetricKey == "Help.Opened").MetricCount.Should().Be(2);
				endedSession.SessionActiveMinutes.Should().Be(0.667);
			}
			finally
			{
				File.Delete(sourcePath);
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
				tracker.StartSession(
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
				tracker.StartSession(
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
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), sourcePath);

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
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), sourcePath);

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
		public void GivenGapLongerThanLease_WhenSessionEnds_ThenGapIsCappedToLeaseDuration()
		{
			var start = new DateTime(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[]
			{
				start,
				start.AddMinutes(5),
				start.AddMinutes(50),
			});
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(15));

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSession("WeevilCli.exe", new Version(2, 0), sourcePath);
				tracker.RecordActivity(TelemetryActivityKind.RecordSelectionChanged);
				var endedSession = tracker.EndSession();

				endedSession.Should().NotBeNull();
				endedSession!.SessionActiveMinutes.Should().Be(20);
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenReadingIntervalsWithinLease_WhenSessionEnds_ThenReadingTimeIsCounted()
		{
			var start = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[]
			{
				start,
				start.AddMinutes(8),
				start.AddMinutes(12),
			});
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(15));
			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), sourcePath);
				tracker.RecordActivity(TelemetryActivityKind.RecordSelectionChanged);
				var ended = tracker.EndSession();

				ended.Should().NotBeNull();
				ended!.SessionActiveMinutes.Should().Be(12);
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenRepeatedScrollingActivity_WhenSessionEnds_ThenActiveUsageExtendsAcrossIntervals()
		{
			var start = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[]
			{
				start,
				start.AddMinutes(10),
				start.AddMinutes(20),
				start.AddMinutes(30),
			});
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(15));
			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), sourcePath);
				tracker.RecordActivity(TelemetryActivityKind.ViewportChanged);
				tracker.RecordActivity(TelemetryActivityKind.ViewportChanged);
				var ended = tracker.EndSession();

				ended.Should().NotBeNull();
				ended!.SessionActiveMinutes.Should().Be(30);
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenLunchGapBetweenActivities_WhenSessionEnds_ThenLunchGapIsCapped()
		{
			var start = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[]
			{
				start,
				start.AddMinutes(5),
				start.AddMinutes(45),
				start.AddMinutes(50),
			});
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(15));
			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), sourcePath);
				tracker.RecordActivity(TelemetryActivityKind.FilterApplied);
				tracker.RecordActivity(TelemetryActivityKind.RecordSelectionChanged);
				var ended = tracker.EndSession();

				ended.Should().NotBeNull();
				ended!.SessionActiveMinutes.Should().Be(25);
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenOvernightSessionEnd_WhenSessionEnds_ThenOvernightGapIsCapped()
		{
			var start = new DateTime(2026, 6, 1, 23, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[]
			{
				start,
				start.AddMinutes(2),
				start.AddHours(8),
			});
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(15));
			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), sourcePath);
				tracker.RecordActivity(TelemetryActivityKind.RecordSelectionChanged);
				var ended = tracker.EndSession();

				ended.Should().NotBeNull();
				ended!.SessionActiveMinutes.Should().Be(17);
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenNoFurtherActivity_WhenSessionEnds_ThenInactiveWindowIsCappedToLease()
		{
			var start = new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[]
			{
				start,
				start.AddMinutes(40),
			});
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(15));
			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), sourcePath);
				var ended = tracker.EndSession();

				ended.Should().NotBeNull();
				ended!.SessionActiveMinutes.Should().Be(15);
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
				tracker.StartSession("WeevilGui.exe", new Version(3, 1), sourcePath);
				tracker.Increment(TelemetryMetrics.FilterApplied);
				tracker.Increment(TelemetryMetrics.FilterApplied);
				var endedSession = tracker.EndSession();

				endedSession.Should().NotBeNull();
				endedSession!.Metrics.Single(m => m.MetricKey == "Filter.Applied").MetricCount.Should().Be(2);
				endedSession.SessionActiveMinutes.Should().Be(0.667);
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenActiveSession_WhenNewFileOpened_ThenPreviousSessionIsSavedAndUploadTriggered()
		{
			var t0 = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
			var t1 = t0.AddSeconds(10);
			var times = new Queue<DateTime>(new[] { t0, t1 });
			var store = new SpyTelemetrySessionStore();
			var uploadWorker = new SpyTelemetryUploadWorker();
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1), store, uploadWorker);

			var firstPath = Path.GetTempFileName();
			var secondPath = Path.GetTempFileName();

			try
			{
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), firstPath);
				var firstSessionId = tracker.CurrentSession.SessionId;
				uploadWorker.Reset();

				tracker.StartSession("WeevilGui.exe", new Version(1, 0), secondPath);

				store.SavedSessions.Should().ContainSingle()
					.Which.SessionId.Should().Be(firstSessionId);
				uploadWorker.TriggerCount.Should().Be(1);
			}
			finally
			{
				File.Delete(firstPath);
				File.Delete(secondPath);
			}
		}

		[TestMethod]
		public void GivenActiveSession_WhenEndSessionCalled_ThenSessionIsSavedWithoutTriggeringUpload()
		{
			var start = new DateTime(2026, 4, 1, 13, 0, 0, DateTimeKind.Utc);
			var times = new Queue<DateTime>(new[] { start, start.AddSeconds(5) });
			var store = new SpyTelemetrySessionStore();
			var uploadWorker = new SpyTelemetryUploadWorker();
			var tracker = new TelemetrySessionLifecycle(() => times.Dequeue(), TimeSpan.FromMinutes(1), store, uploadWorker);

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSession("WeevilCli.exe", new Version(2, 0), sourcePath);
				var sessionId = tracker.CurrentSession.SessionId;
				uploadWorker.Reset();

				var endedSession = tracker.EndSession();

				store.SavedSessions.Should().ContainSingle()
					.Which.SessionId.Should().Be(sessionId);
				uploadWorker.TriggerCount.Should().Be(0);
				endedSession.Should().NotBeNull();
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenNoActiveSession_WhenEndSessionCalled_ThenNoSaveOrUploadOccurs()
		{
			var store = new SpyTelemetrySessionStore();
			var uploadWorker = new SpyTelemetryUploadWorker();
			var tracker = new TelemetrySessionLifecycle(() => DateTime.UtcNow, TimeSpan.FromMinutes(1), store, uploadWorker);

			tracker.EndSession();

			store.SavedSessions.Should().BeEmpty();
			uploadWorker.TriggerCount.Should().Be(0);
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
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), sourcePath);

				Action act = () => tracker.EndSession();

				act.Should().NotThrow();
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		[TestMethod]
		public void GivenTelemetryConsentDisabled_WhenSessionStartsAndEnds_ThenSessionIsTrackedButNotSavedOrUploaded()
		{
			// Regression: Issue #919
			var start = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
			var activityRecordedAt = start.AddSeconds(20);
			var times = new Queue<DateTime>(new[] { start, activityRecordedAt, activityRecordedAt });
			var store = new SpyTelemetrySessionStore();
			var uploadWorker = new SpyTelemetryUploadWorker();
			var tracker = new TelemetrySessionLifecycle(
				() => times.Dequeue(),
				TimeSpan.FromMinutes(1),
				store,
				uploadWorker,
				() => false);

			var sourcePath = Path.GetTempFileName();

			try
			{
				tracker.StartSession("WeevilGui.exe", new Version(1, 0), sourcePath);
				tracker.RecordHelpOpen();
				var endedSession = tracker.EndSession();

				tracker.CurrentSession.Should().BeNull();
				endedSession.Should().NotBeNull();
				endedSession!.Metrics.Single(m => m.MetricKey == TelemetryMetrics.HelpOpened).MetricCount.Should().Be(1);
				endedSession.SessionActiveMinutes.Should().Be(0.333);
				store.SavedSessions.Should().BeEmpty();
				uploadWorker.TriggerCount.Should().Be(0);
			}
			finally
			{
				File.Delete(sourcePath);
			}
		}

		private sealed class SpyTelemetrySessionStore : ITelemetrySessionStore
		{
			public List<TelemetrySessionDto> SavedSessions { get; } = new List<TelemetrySessionDto>();

			public void Save(TelemetrySessionDto session)
			{
				SavedSessions.Add(session);
			}

			public IReadOnlyList<PendingTelemetrySession> GetPendingSessions(int maxCount)
			{
				return Array.Empty<PendingTelemetrySession>();
			}

			public void Delete(PendingTelemetrySession session)
			{
				// no-op
			}
		}

		private sealed class SpyTelemetryUploadWorker : ITelemetryUploadWorker
		{
			public int TriggerCount { get; private set; }

			public void TriggerUpload()
			{
				TriggerCount++;
			}

			public void Reset()
			{
				TriggerCount = 0;
			}
		}
	}
}