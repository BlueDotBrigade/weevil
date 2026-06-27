namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Uploads pending telemetry sessions in the background without blocking the UI.
	/// </summary>
	public sealed class TelemetryUploadWorker : ITelemetryUploadWorker
	{
		private const int DefaultMaxRetryAttempts = 3;
		private const int DefaultMaxPendingSessionsPerBatch = 100;

		private readonly Func<ITelemetryClient> _clientAccessor;
		private readonly ITelemetrySessionStore _sessionStore;
		private readonly int _maxRetryAttempts;
		private readonly int _maxPendingSessionsPerBatch;
		private readonly TimeSpan _retryDelay;
		private readonly object _taskGate;
		private Task _activeUploadTask;
		private int _isUploadRunning;
		private int _isDisabled;

		public TelemetryUploadWorker(
			Func<ITelemetryClient> clientAccessor,
			ITelemetrySessionStore sessionStore,
			int maxRetryAttempts = DefaultMaxRetryAttempts,
			int maxPendingSessionsPerBatch = DefaultMaxPendingSessionsPerBatch,
			TimeSpan? retryDelay = null)
		{
			_clientAccessor = clientAccessor ?? throw new ArgumentNullException(nameof(clientAccessor));
			_sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
			_maxRetryAttempts = maxRetryAttempts > 0 ? maxRetryAttempts : throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts));
			_maxPendingSessionsPerBatch = maxPendingSessionsPerBatch > 0 ? maxPendingSessionsPerBatch : throw new ArgumentOutOfRangeException(nameof(maxPendingSessionsPerBatch));
			// Azure SQL serverless may be paused: the first attempt typically fails while the instance
			// resumes, so wait long enough for the resume to complete before retrying.
			_retryDelay = retryDelay ?? TimeSpan.FromMinutes(2);
			_taskGate = new object();
			_activeUploadTask = Task.CompletedTask;
		}

		internal Task ActiveUploadTask
		{
			get
			{
				lock (_taskGate)
				{
					return _activeUploadTask;
				}
			}
		}

		public void TriggerUpload()
		{
			if (Volatile.Read(ref _isDisabled) == 1)
			{
				return;
			}

			if (Interlocked.CompareExchange(ref _isUploadRunning, 1, 0) != 0)
			{
				return;
			}

			var uploadTask = Task.Run(UploadPendingSessionsAsync);

			lock (_taskGate)
			{
				_activeUploadTask = uploadTask;
			}
		}

		private async Task UploadPendingSessionsAsync()
		{
			try
			{
				foreach (var pendingSession in _sessionStore.GetPendingSessions(_maxPendingSessionsPerBatch))
				{
					var uploadStatus = await UploadWithRetryAsync(pendingSession).ConfigureAwait(false);

					switch (uploadStatus)
					{
						case TelemetryUploadStatus.Success:
						case TelemetryUploadStatus.DuplicateSession:
							DeletePendingSession(pendingSession);
							break;
						case TelemetryUploadStatus.InvalidCredentials:
							Interlocked.Exchange(ref _isDisabled, 1);
							return;
						case TelemetryUploadStatus.Disabled:
							return;
						case TelemetryUploadStatus.Failed:
							break;
					}
				}
			}
			catch (Exception exception)
			{
				Log.Default.Write(LogSeverityType.Warning, exception, "Telemetry background upload failed.");
			}
			finally
			{
				Interlocked.Exchange(ref _isUploadRunning, 0);
			}
		}

		private async Task<TelemetryUploadStatus> UploadWithRetryAsync(PendingTelemetrySession pendingSession)
		{
			for (var attempt = 1; attempt <= _maxRetryAttempts; attempt++)
			{
				var client = _clientAccessor() ?? NullTelemetryClient.Instance;
				var uploadStatus = await client
					.UploadAsync(TelemetrySessionMapper.ToSession(pendingSession.Session), CancellationToken.None)
					.ConfigureAwait(false);

				if (uploadStatus != TelemetryUploadStatus.Failed || attempt == _maxRetryAttempts)
				{
					return uploadStatus;
				}

				await Task.Delay(_retryDelay).ConfigureAwait(false);
			}

			return TelemetryUploadStatus.Failed;
		}

		private void DeletePendingSession(PendingTelemetrySession pendingSession)
		{
			try
			{
				_sessionStore.Delete(pendingSession);
			}
			catch (Exception exception)
			{
				Log.Default.Write(LogSeverityType.Warning, exception, $"Telemetry XML delete failed for '{pendingSession.FilePath}'.");
			}
		}
	}
}
