namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.IO;
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
				tracker.RecordCliCommandExecution();
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
	}
}
