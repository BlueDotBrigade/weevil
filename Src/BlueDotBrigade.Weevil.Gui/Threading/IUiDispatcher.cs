namespace BlueDotBrigade.Weevil.Gui.Threading
{
	using System;
	using System.Windows.Threading;

	internal interface IUiDispatcher
	{
		void Invoke(Action callback);
		void Invoke(Action callback, DispatcherPriority priority);
	}
}
