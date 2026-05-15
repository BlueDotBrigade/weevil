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
	///   <item><description>Shutdown/crash (<see cref="EndSession"/>): synchronous, best-effort.</description></item>
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
		private StartupContext _startupContext;

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
			_startupContext = StartupContext.Default;
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
		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void Configure(ITelemetryClient client)
		{
			try
			{
				lock (_gate)
				{
					_client = client ?? NullTelemetryClient.Instance;
				}
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry configure failed.");
			}
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void StartSession(
			string application,
			Version version,
			string sourceFilePath,
			long installedRamMb = 0,
			string installedCpu = "")
		{
			try
			{
				if (string.IsNullOrWhiteSpace(sourceFilePath))
				{
					return;
				}

				ITelemetryClient client;
				TelemetrySession endedSession;

				lock (_gate)
				{
					client = _client;
					var now = _utcNow();
					endedSession = EndCurrentSessionInternal(now);

					CurrentSession = new TelemetrySession
					{
						SessionId = Guid.NewGuid(),
						Application = string.IsNullOrWhiteSpace(application) ? "Weevil" : application,
						Source = _startupContext.Source,
						Version = version ?? new Version(0, 0),
						IsDebugging = _startupContext.IsDebugging,
						SessionStartUtc = now,
						SessionEndUtc = now,
						LogFileSizeBytes = TryGetFileSize(sourceFilePath),
                      InstalledRamMb = installedRamMb,
						InstalledCpu = string.IsNullOrWhiteSpace(installedCpu) ? "" : installedCpu,
						SchemaVersion = "1.0",
					};
					_lastActivityUtc = now;
				}

				// Warm up the database connection on the thread pool (fire-and-forget).
				client.Warmup();

				// Async upload on rollover: must not block the file-open flow.
				if (endedSession != null)
				{
					_ = SafeSendAsync(endedSession);
				}
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry start session failed.");
			}
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public TelemetrySession EndSession()
		{
			TelemetrySession endedSession = null;

			try
			{
				lock (_gate)
				{
					endedSession = EndCurrentSessionInternal(_utcNow());
				}

				// Sync best-effort upload on shutdown/crash.
				if (endedSession != null)
				{
					SafeSendSync(endedSession);
				}
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry end session failed.");
			}

			return endedSession;
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void RecordSessionHeartbeat()
		{
			try
			{
				RecordActivity();
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry heartbeat recording failed.");
			}
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void RecordFilterExecution()
		{
			try
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
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry filter execution recording failed.");
			}
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void RecordNavigationAction()
		{
			try
			{
				RecordActivity();
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry navigation action recording failed.");
			}
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void RecordRecordAction()
		{
			try
			{
				RecordActivity();
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry record action recording failed.");
			}
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void RecordDashboardOpen()
		{
			try
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
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry dashboard open recording failed.");
			}
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void RecordGraphOpen()
		{
			try
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
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry graph open recording failed.");
			}
		}
#pragma warning restore CA1031

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

		public void ConfigureStartupContext(string source, bool isDebugging)
		{
			lock (_gate)
			{
				_startupContext = new StartupContext(
					string.IsNullOrWhiteSpace(source) ? "unknown" : source,
					isDebugging);
			}
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

		// Intentional broad exception catching: logging failures must never propagate to the user workflow.
#pragma warning disable CA1031
		private static void TrySilentlyLogWarning(Exception exception, string message)
		{
			try
			{
				Log.Default.Write(LogSeverityType.Warning, exception, message);
			}
			catch
			{
				// Nothing to do.
			}
		}
#pragma warning restore CA1031

		private sealed class StartupContext
		{
			public StartupContext(string source, bool isDebugging)
			{
				Source = source;
				IsDebugging = isDebugging;
			}

			public static StartupContext Default => new StartupContext("unknown", false);

			public string Source { get; }

			public bool IsDebugging { get; }
		}
	}
}
