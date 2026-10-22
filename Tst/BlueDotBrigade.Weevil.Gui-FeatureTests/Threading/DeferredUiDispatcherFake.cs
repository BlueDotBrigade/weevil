namespace BlueDotBrigade.Weevil.Gui.Threading
{
	using System;
	using System.Collections.Generic;
	using System.Windows.Threading;

	/// <summary>
	/// Mimics a UI thread that is busy: <see cref="BeginInvoke"/> queues its callback until <see cref="RunQueued"/> is called.
	/// </summary>
	internal class DeferredUiDispatcherFake : IUiDispatcher
	{
		private readonly Queue<Action> _queued = new Queue<Action>();

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
			while (_queued.Count > 0)
			{
				_queued.Dequeue().Invoke();
			}
		}
	}
}
