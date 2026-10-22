namespace BlueDotBrigade.Weevil.Gui.Threading
{
	using System;
	using System.Windows.Threading;

	internal interface IUiDispatcher
	{
		void Invoke(Action callback);
		void Invoke(Action callback, DispatcherPriority priority);

		/// <summary>
		/// Queues the callback on the UI thread and returns at once.
		/// </summary>
		void BeginInvoke(Action callback, DispatcherPriority priority);
	}
}
