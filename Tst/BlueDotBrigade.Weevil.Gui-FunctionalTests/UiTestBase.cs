namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.TestingTools;

	public abstract class UiTestBase 
	{
		internal IUiDispatcher UiDispatcher { get; private set; }

		public UiTestBase()
		{
			this.UiDispatcher = new UiDispatcherFake();
		}
	}
}
