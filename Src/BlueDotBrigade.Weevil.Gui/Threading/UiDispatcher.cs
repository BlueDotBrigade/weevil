namespace BlueDotBrigade.Weevil.Gui.Threading
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Threading;

	internal class UiDispatcher : IUiDispatcher 
	{
		private readonly Dispatcher _dispatcher;

		public UiDispatcher(Dispatcher dispatcher)
		{
			_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		}

		public void Invoke(Action callback)
		{
			_dispatcher.Invoke(callback);
		}

		public void Invoke(Action callback, DispatcherPriority priority)
		{
			_dispatcher.Invoke(callback, priority);
		}
	}
}
