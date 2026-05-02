namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Windows;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.Gui.Navigation;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using Metalama.Patterns.Observability;

	[Observable]
	internal class MainWindowViewModel
	{
		private readonly IUiDispatcher _uiDispatcher;
		private readonly UiResponsivenessMonitor _uiMonitor;
		private readonly TelemetrySessionLifecycle _telemetry;
		private readonly Version _applicationVersion;

		public MainWindowViewModel(IUiDispatcher uiDispatcher, IBulletinMediator bulletinMediator)
		{
			_uiDispatcher = uiDispatcher;
			_uiMonitor = new UiResponsivenessMonitor();
			_telemetry = TelemetrySessionLifecycle.Shared;

			bulletinMediator.Subscribe<SourceFileOpenedBulletin>(this, x => OnSourceFileChanged(x));
			bulletinMediator.Subscribe<FilterChangedBulletin>(this, _ => _telemetry.RecordFilterExecution());
			bulletinMediator.Subscribe<SelectionChangedBulletin>(this, _ => _telemetry.RecordNavigationAction());
			bulletinMediator.Subscribe<BookmarksChangedBulletin>(this, _ => _telemetry.RecordRecordAction());
			bulletinMediator.Subscribe<RegionsChangedBulletin>(this, _ => _telemetry.RecordRecordAction());

			this.FilterViewModel = new FilterViewModel(
				uiDispatcher,
				bulletinMediator);
			_applicationVersion = this.FilterViewModel.WeevilVersion;

			this.StatusBarViewModel = new StatusBarViewModel(
				uiDispatcher,
				bulletinMediator);

          this.ApplicationTitle = $"Weevil: v{this.FilterViewModel.WeevilDisplayVersion}";
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
			_telemetry.EndCurrentSession();
			_uiMonitor.Stop();
		}

		private void OnSourceFileChanged(SourceFileOpenedBulletin bulletin)
		{
			var title = Path.GetFileNameWithoutExtension(bulletin.SourceFilePath);

			_uiDispatcher.Invoke(() => this.ApplicationTitle = title);
			_telemetry.StartSessionOnFileOpen("WeevilGui.exe", _applicationVersion, bulletin.SourceFilePath);
		}

		public FilterViewModel FilterViewModel { get; }

		public StatusBarViewModel StatusBarViewModel { get; }

		public string ApplicationTitle { get; set; }
	}
}
