namespace BlueDotBrigade.Weevil.Gui.Threading
{
	using System;
	using System.Collections.Concurrent;
	using System.Windows.Threading;

	/// <summary>
	/// Mimics a UI thread that is busy: <see cref="BeginInvoke"/> queues its callback until <see cref="RunQueued"/> is called.
	/// </summary>
	/// <remarks>
	/// Background threads may queue callbacks while the test runs them, as a Reload does.
	/// </remarks>
	internal class DeferredUiDispatcherFake : IUiDispatcher
	{
		private readonly ConcurrentQueue<Action> _queued = new ConcurrentQueue<Action>();

		public int QueuedCount => _queued.Count;

		public void Invoke(Action callback)
		{
			callback.Invoke();
		}

		public void Invoke(Action callback, DispatcherPriority priority)
		{
			callback.Invoke();
		}

		public void BeginInvoke(Action callback, DispatcherPriority priority)
		{
			_queued.Enqueue(callback);
		}

		public void RunQueued()
		{
			while (_queued.TryDequeue(out Action callback))
			{
				callback.Invoke();
			}
		}
	}
}
