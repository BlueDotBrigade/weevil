namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.Windows;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using Metalama.Patterns.Observability;

	[Observable]
	internal class MainWindowViewModel
	{
		private readonly IUiDispatcher _uiDispatcher;
		private readonly UiResponsivenessMonitor _uiMonitor;

		public MainWindowViewModel(IUiDispatcher uiDispatcher, IBulletinMediator bulletinMediator)
		{
			_uiDispatcher = uiDispatcher;
			_uiMonitor = new UiResponsivenessMonitor();

			bulletinMediator.Subscribe<SourceFileOpenedBulletin>(this, x => OnSourceFileChanged(x));

			this.FilterViewModel = new FilterViewModel(
				uiDispatcher,
				bulletinMediator);

			this.StatusBarViewModel = new StatusBarViewModel(
				uiDispatcher,
				bulletinMediator);

			Version weevilVersion = Assembly.GetEntryAssembly()?.GetName().Version;
			weevilVersion = weevilVersion ?? new Version(128, 128, 128);
			this.ApplicationTitle = $"Weevil: v{weevilVersion.ToString(3)}";
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

		private void OnSourceFileChanged(SourceFileOpenedBulletin bulletin)
		{
			var title = Path.GetFileNameWithoutExtension(bulletin.SourceFilePath);

			_uiDispatcher.Invoke(() => this.ApplicationTitle = title);
		}

		public FilterViewModel FilterViewModel { get; }

		public StatusBarViewModel StatusBarViewModel { get; }

		public string ApplicationTitle { get; set; }
	}
}