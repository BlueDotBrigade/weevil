namespace BlueDotBrigade.Weevil.Gui
{
	using System.Diagnostics;
	using System.Windows;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.Threading;

	internal class MainWindowViewModel
	{
		private readonly UiResponsivenessMonitor _uiMonitor;
		private readonly BulletinMediator _bulletinMediator;

		public MainWindowViewModel()
		{
			_uiMonitor = new UiResponsivenessMonitor();
			_bulletinMediator = new BulletinMediator();

			this.CurrrentFilter = new FilterResultsViewModel(
				Application.Current.MainWindow,
				new UiDispatcher(Application.Current.Dispatcher),
				_bulletinMediator);

			this.CurrentStatus = new MainStatusBarViewModel(
				new UiDispatcher(Application.Current.Dispatcher),
				_bulletinMediator);
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

		public FilterResultsViewModel CurrrentFilter { get; }

		public MainStatusBarViewModel CurrentStatus { get; }
	}
}
