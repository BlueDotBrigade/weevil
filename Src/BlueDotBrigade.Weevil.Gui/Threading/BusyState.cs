namespace BlueDotBrigade.Weevil.Gui.Threading
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Diagnostics;

	/// <summary>
	/// The one operation that keeps Weevil busy, such as an open, a filter or an analysis: Open, the menus, the filter box and the progress bar follow it (#1257).
	/// </summary>
	/// <remarks>
	/// One runs at a time. An operation begins and ends on the UI thread, so <see cref="Changed"/> is raised there.
	/// </remarks>
	internal sealed class BusyState
	{
		private readonly object _padlock = new object();

		private volatile Operation _current;

		/// <summary>
		/// Raised when an operation begins or ends, and when the one running changes what it shows, on the thread that changed it.
		/// </summary>
		public event EventHandler Changed;

		public bool IsBusy => _current != null;

		/// <summary>
		/// The progress bar replaces the records.
		/// </summary>
		public bool IsShowingProgress => _current?.IsShowingProgress == true;

		/// <summary>
		/// The filter box is disabled, because the operation changes the records or reads the log.
		/// </summary>
		public bool IsFilterLocked => _current?.IsFilterLocked == true;

		/// <summary>
		/// Abort (Escape) can stop the operation, through its <see cref="Operation.CancellationToken"/>.
		/// </summary>
		public bool CanCancel => _current?.CanCancel == true;

		/// <summary>
		/// Completes when the operation running now ends, or at once when none runs.
		/// </summary>
		public Task Idle => _current?.Ended ?? Task.CompletedTask;

		/// <summary>
		/// Begins an operation, unless another is running.
		/// </summary>
		/// <param name="name">What the operation does, for the log, such as "The analysis".</param>
		/// <returns>The operation, which ends when it is disposed, or <see langword="null"/> when another is running.</returns>
		public Operation TryBegin(string name, bool isShowingProgress, bool isFilterLocked, bool canCancel = false)
		{
			var operation = new Operation(this, name, isShowingProgress, isFilterLocked, canCancel);

			lock (_padlock)
			{
				if (_current != null)
				{
					return null;
				}

				_current = operation;
			}

			Log.Default.Write(LogSeverityType.Debug, $"{name} has begun.");
			this.Changed?.Invoke(this, EventArgs.Empty);

			return operation;
		}

		/// <summary>
		/// Cancels the token of the operation running, if any.
		/// </summary>
		public void Cancel()
		{
			_current?.Cancel();
		}

		private void End(Operation operation)
		{
			lock (_padlock)
			{
				if (_current != operation)
				{
					return;
				}

				_current = null;
			}

			Log.Default.Write(LogSeverityType.Debug, $"{operation.Name} has ended.");
			this.Changed?.Invoke(this, EventArgs.Empty);
			operation.Complete();
		}

		private void OnChanged(Operation operation)
		{
			if (_current == operation)
			{
				this.Changed?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// An operation that keeps Weevil busy until it is disposed. Disposing it again, or after another has begun, does nothing.
		/// </summary>
		internal sealed class Operation : IDisposable
		{
			private readonly BusyState _owner;
			private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

			// Its awaiters continue on another thread, so none runs inside the dispatcher callback that ended it.
			private readonly TaskCompletionSource _ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			private bool _isShowingProgress;
			private bool _isFilterLocked;

			internal Operation(BusyState owner, string name, bool isShowingProgress, bool isFilterLocked, bool canCancel)
			{
				_owner = owner;
				_isShowingProgress = isShowingProgress;
				_isFilterLocked = isFilterLocked;
				this.Name = name;
				this.CanCancel = canCancel;
			}

			public string Name { get; }

			public bool CanCancel { get; }

			/// <summary>
			/// Cancelled when Abort (Escape) is pressed, if <see cref="CanCancel"/>.
			/// </summary>
			public CancellationToken CancellationToken => _cancellation.Token;

			/// <summary>
			/// Completes once the operation has ended.
			/// </summary>
			public Task Ended => _ended.Task;

			public bool IsShowingProgress
			{
				get => _isShowingProgress;
				set
				{
					if (_isShowingProgress != value)
					{
						_isShowingProgress = value;
						_owner.OnChanged(this);
					}
				}
			}

			public bool IsFilterLocked
			{
				get => _isFilterLocked;
				set
				{
					if (_isFilterLocked != value)
					{
						_isFilterLocked = value;
						_owner.OnChanged(this);
					}
				}
			}

			public void Dispose()
			{
				_owner.End(this);
			}

			internal void Cancel()
			{
				if (this.CanCancel)
				{
					_cancellation.Cancel();
				}
			}

			internal void Complete()
			{
				_ended.TrySetResult();
			}
		}
	}
}
