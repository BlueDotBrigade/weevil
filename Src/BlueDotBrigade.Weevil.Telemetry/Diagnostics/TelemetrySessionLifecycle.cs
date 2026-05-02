namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Tracks telemetry session lifecycle and active time based on meaningful activity.
	/// </summary>
	/// <remarks>
	/// Upload behavior:
	/// <list type="bullet">
	///   <item><description>Rollover (new file open): asynchronous, non-blocking.</description></item>
	///   <item><description>Shutdown/crash (<see cref="EndCurrentSession"/>): synchronous, best-effort.</description></item>
	/// </list>
	/// Exactly-once semantics are enforced naturally: a session can only be ended once,
	/// so each ended session is dispatched to the client exactly once.
	/// </remarks>
	public sealed class TelemetrySessionLifecycle
	{
		private readonly object _gate;
		private readonly Func<DateTime> _utcNow;
		private readonly TimeSpan _idleThreshold;
		private DateTime? _lastActivityUtc;
		private ITelemetryClient _client;

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
			_client = NullTelemetryClient.Instance;
		}

		public static TelemetrySessionLifecycle Shared { get; } = new TelemetrySessionLifecycle();

		public TelemetrySession CurrentSession { get; private set; }

		public TelemetrySession LastEndedSession { get; private set; }

		/// <summary>
		/// Configures the telemetry client used to upload session data.
		/// </summary>
		/// <param name="client">
		/// The client to use. Passing <see langword="null"/> restores the no-op default.
		/// </param>
		public void Configure(ITelemetryClient client)
		{
			lock (_gate)
			{
				_client = client ?? NullTelemetryClient.Instance;
			}
		}

		public void StartSessionOnFileOpen(string application, Version version, string sourceFilePath, long installedRamMb = 0)
		{
			if (string.IsNullOrWhiteSpace(sourceFilePath))
			{
				return;
			}

			TelemetrySession endedSession;

			lock (_gate)
			{
				var now = _utcNow();
				endedSession = EndCurrentSessionInternal(now);

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

			// Async upload on rollover: must not block the file-open flow.
			if (endedSession != null)
			{
				_ = SafeSendAsync(endedSession);
			}
		}

		public TelemetrySession EndCurrentSession()
		{
			TelemetrySession endedSession;

			lock (_gate)
			{
				endedSession = EndCurrentSessionInternal(_utcNow());
			}

			// Sync best-effort upload on shutdown/crash.
			if (endedSession != null)
			{
				SafeSendSync(endedSession);
			}

			return endedSession;
		}

		public void RecordSessionHeartbeat()
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

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		private async Task SafeSendAsync(TelemetrySession session)
		{
			ITelemetryClient client;

			lock (_gate)
			{
				client = _client;
			}

			try
			{
				await client.SendAsync(session, CancellationToken.None).ConfigureAwait(false);
			}
			catch (Exception exception)
			{
				try
				{
					Log.Default.Write(LogSeverityType.Warning, exception, "Telemetry async upload failed.");
				}
				catch
				{
					// Nothing to do.
				}
			}
		}

		private void SafeSendSync(TelemetrySession session)
		{
			ITelemetryClient client;

			lock (_gate)
			{
				client = _client;
			}

			try
			{
				client.SendSync(session);
			}
			catch (Exception exception)
			{
				try
				{
					Log.Default.Write(LogSeverityType.Warning, exception, "Telemetry sync upload failed.");
				}
				catch
				{
					// Nothing to do.
				}
			}
		}
#pragma warning restore CA1031

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
