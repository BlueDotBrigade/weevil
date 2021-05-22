namespace BlueDotBrigade.Weevil.Gui
{
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Analysis;

	internal class MainWindowViewModel
	{
		private readonly UiResponsivenessMonitor _uiMonitor;

		public MainWindowViewModel()
		{
			_uiMonitor = new UiResponsivenessMonitor();
		}

		public void Start()
		{
			if (Debugger.IsAttached)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					"The request to start UI monitoring is being ignored because the Visual Studio debugger is attached to this process.");
			}
			else
			{
				_uiMonitor.Start();
			}
		}

		public void Stop()
		{
			_uiMonitor.Stop();
		}
	}
}
