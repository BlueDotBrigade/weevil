namespace BlueDotBrigade.Weevil
{
	using System;
	using System.IO;

	/// <summary>
	/// Tracks telemetry session lifecycle and active time based on meaningful activity.
	/// </summary>
	public sealed class TelemetrySessionLifecycle
	{
		private readonly object _gate;
		private readonly Func<DateTime> _utcNow;
		private readonly TimeSpan _idleThreshold;
		private DateTime? _lastActivityUtc;

		public TelemetrySessionLifecycle() : this(() => DateTime.UtcNow, TimeSpan.FromMinutes(1))
		{
		}

		public TelemetrySessionLifecycle(Func<DateTime> utcNow, TimeSpan idleThreshold)
		{
			_gate = new object();
			_utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
			_idleThreshold = idleThreshold <= TimeSpan.Zero
				? throw new ArgumentOutOfRangeException(nameof(idleThreshold))
				: idleThreshold;
		}

		public static TelemetrySessionLifecycle Shared { get; } = new TelemetrySessionLifecycle();

		public TelemetrySession CurrentSession { get; private set; }

		public TelemetrySession LastEndedSession { get; private set; }

		public void StartSessionOnFileOpen(string application, Version version, string sourceFilePath, long installedRamMb = 0)
		{
			if (string.IsNullOrWhiteSpace(sourceFilePath))
			{
				return;
			}

			lock (_gate)
			{
				var now = _utcNow();
				EndCurrentSessionInternal(now);

				CurrentSession = new TelemetrySession
				{
					SessionId = Guid.NewGuid(),
					Application = string.IsNullOrWhiteSpace(application) ? "unknown" : application,
					Version = version ?? new Version(0, 0),
					SessionStartUtc = now,
					SessionEndUtc = now,
					LogFileSizeBytes = TryGetFileSize(sourceFilePath),
					InstalledRamMb = installedRamMb,
					SchemaVersion = "1.0",
				};
				_lastActivityUtc = now;
			}
		}

		public TelemetrySession EndCurrentSession()
		{
			lock (_gate)
			{
				return EndCurrentSessionInternal(_utcNow());
			}
		}

		public void RecordCliCommandExecution()
		{
			RecordActivity();
		}

		public void RecordFilterExecution()
		{
			lock (_gate)
			{
				RecordActivityInternal(_utcNow());

				if (CurrentSession is not null)
				{
					CurrentSession.FilterExecutionCount++;
				}
			}
		}

		public void RecordNavigationAction()
		{
			RecordActivity();
		}

		public void RecordRecordAction()
		{
			RecordActivity();
		}

		public void RecordDashboardOpen()
		{
			lock (_gate)
			{
				RecordActivityInternal(_utcNow());

				if (CurrentSession is not null)
				{
					CurrentSession.DashboardOpenCount++;
				}
			}
		}

		public void RecordGraphOpen()
		{
			lock (_gate)
			{
				RecordActivityInternal(_utcNow());

				if (CurrentSession is not null)
				{
					CurrentSession.GraphOpenCount++;
				}
			}
		}

		private void RecordActivity()
		{
			lock (_gate)
			{
				RecordActivityInternal(_utcNow());
			}
		}

		private void RecordActivityInternal(DateTime now)
		{
			if (CurrentSession is null)
			{
				return;
			}

			AccumulateActiveMinutes(now);
			_lastActivityUtc = now;
		}

		private TelemetrySession EndCurrentSessionInternal(DateTime endedAtUtc)
		{
			if (CurrentSession is null)
			{
				return null;
			}

			AccumulateActiveMinutes(endedAtUtc);

			CurrentSession.SessionEndUtc = endedAtUtc;
			CurrentSession.SessionActiveMinutes = System.Math.Round(CurrentSession.SessionActiveMinutes, 3);

			LastEndedSession = CurrentSession;
			CurrentSession = null;
			_lastActivityUtc = null;

			return LastEndedSession;
		}

		private void AccumulateActiveMinutes(DateTime now)
		{
			if (_lastActivityUtc is null || CurrentSession is null)
			{
				return;
			}

			var elapsed = now - _lastActivityUtc.Value;

			if (elapsed <= TimeSpan.Zero || elapsed > _idleThreshold)
			{
				return;
			}

			CurrentSession.SessionActiveMinutes += elapsed.TotalMinutes;
		}

		private static long TryGetFileSize(string sourceFilePath)
		{
			try
			{
				return new FileInfo(sourceFilePath).Length;
			}
			catch (Exception exception) when (
				exception is UnauthorizedAccessException ||
				exception is IOException ||
				exception is NotSupportedException ||
				exception is ArgumentException)
			{
				return 0;
			}
		}
	}
}
