namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.IO;

	/// <summary>
	/// Tracks telemetry session lifecycle and active time based on meaningful activity.
	/// </summary>
	/// <remarks>
	/// Ended sessions are first written to the local XML outbox. Upload happens only in the background
	/// when safe entry points such as log open trigger the upload worker.
	/// </remarks>
	public sealed class TelemetrySessionLifecycle
	{
		private readonly object _gate;
		private readonly Func<DateTime> _utcNow;
		private readonly TelemetryActiveUsageAccumulator _activeUsageAccumulator;
		private readonly ITelemetrySessionStore _sessionStore;
		private readonly ITelemetryUploadWorker _uploadWorker;
		private ITelemetryClient _client;
		private StartupContext _startupContext;
		private static readonly TimeSpan DefaultActivityLeaseDuration = TimeSpan.FromMinutes(15);

		public TelemetrySessionLifecycle() : this(() => DateTime.UtcNow, DefaultActivityLeaseDuration)
		{
		}

		public TelemetrySessionLifecycle(
			Func<DateTime> utcNow,
			TimeSpan activityLeaseDuration,
			ITelemetrySessionStore sessionStore = null,
			ITelemetryUploadWorker uploadWorker = null)
		{
			_gate = new object();
			_utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
			_activeUsageAccumulator = new TelemetryActiveUsageAccumulator(activityLeaseDuration);
			_sessionStore = sessionStore ?? new TelemetrySessionXmlStore();
			_client = NullTelemetryClient.Instance;
			_uploadWorker = uploadWorker ?? new TelemetryUploadWorker(GetConfiguredClient, _sessionStore);
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
                      SchemaVersion = "2.0",
					};
					_activeUsageAccumulator.Reset(now);
				}

				if (endedSession != null)
				{
					TrySaveEndedSession(endedSession);
				}

				// Warm up the database connection on the thread pool (fire-and-forget).
				client.Warmup();

				_uploadWorker.TriggerUpload();
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

				if (endedSession != null)
				{
					TrySaveEndedSession(endedSession);
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
				RecordActivity(TelemetryActivityKind.Unknown);
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
				RecordActivity(TelemetryActivityKind.FilterApplied);
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry filter execution recording failed.");
			}
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void RecordActivity(TelemetryActivityKind activityKind)
		{
			try
			{
				lock (_gate)
				{
					RecordActivityInternal(_utcNow(), activityKind);
				}
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, $"Telemetry activity recording failed for '{activityKind}'.");
			}
		}
#pragma warning restore CA1031

		// Intentional broad exception catching: telemetry failures must never propagate to the user workflow.
#pragma warning disable CA1031
		public void RecordNavigationAction()
		{
			try
			{
				RecordActivity(TelemetryActivityKind.RecordSelectionChanged);
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
				RecordActivity(TelemetryActivityKind.RecordAnnotationChanged);
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
				RecordActivity(TelemetryActivityKind.DashboardOpen);
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
				RecordActivity(TelemetryActivityKind.GraphOpen);
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry graph open recording failed.");
			}
		}
#pragma warning restore CA1031

		private void RecordActivityInternal(DateTime now, TelemetryActivityKind activityKind)
		{
			if (CurrentSession is null)
			{
				return;
			}

			_activeUsageAccumulator.Renew(now);
			CurrentSession.SessionActiveMinutes = _activeUsageAccumulator.ActiveMinutes;

			switch (activityKind)
			{
				case TelemetryActivityKind.FilterApplied:
					CurrentSession.FilterExecutionCount++;
					break;
				case TelemetryActivityKind.GraphOpen:
					CurrentSession.GraphOpenCount++;
					break;
				case TelemetryActivityKind.DashboardOpen:
					CurrentSession.DashboardOpenCount++;
					break;
			}
		}

		private TelemetrySession EndCurrentSessionInternal(DateTime endedAtUtc)
		{
			if (CurrentSession is null)
			{
				return null;
			}

			_activeUsageAccumulator.Renew(endedAtUtc);

			CurrentSession.SessionEndUtc = endedAtUtc;
			CurrentSession.SessionActiveMinutes = System.Math.Round(_activeUsageAccumulator.ActiveMinutes, 3);

			LastEndedSession = CurrentSession;
			CurrentSession = null;
			_activeUsageAccumulator.Clear();

			return LastEndedSession;
		}

		private ITelemetryClient GetConfiguredClient()
		{
			lock (_gate)
			{
				return _client;
			}
		}

		// Intentional broad exception catching: telemetry persistence failures must never propagate to the user workflow.
#pragma warning disable CA1031
		private void TrySaveEndedSession(TelemetrySession session)
		{
			try
			{
				_sessionStore.Save(TelemetrySessionMapper.ToDto(session));
			}
			catch (Exception exception)
			{
				TrySilentlyLogWarning(exception, "Telemetry XML save failed.");
			}
		}
#pragma warning restore CA1031

		public void ConfigureStartupContext(string source, bool isDebugging)
		{
			lock (_gate)
			{
				_startupContext = new StartupContext(
					string.IsNullOrWhiteSpace(source) ? "unknown" : source,
					isDebugging);
			}
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
