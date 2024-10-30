namespace BlueDotBrigade.Weevil.Gui.Threading
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Threading;

	/// <summary>
	/// Mimics the behavior of a real UI dispatcher, so that 
	/// functional tests can be executed without a user interface.
	/// </summary>
	internal class UiDispatcherFake : IUiDispatcher
	{
		public void Invoke(Action callback)
		{
			callback.Invoke();
		}

		public void Invoke(Action callback, DispatcherPriority priority)
		{
			callback.Invoke();
		}
	}
}