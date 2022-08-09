namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.Windows;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.Gui.Threading;

	internal class MainWindowViewModel : DependencyObject
	{
		private readonly IUiDispatcher _uiDispatcher;

		public static readonly DependencyProperty ApplicationTitleProperty = DependencyProperty.Register(
			nameof(ApplicationTitle),
			typeof(string),
			typeof(MainWindowViewModel)
		);


		private readonly UiResponsivenessMonitor _uiMonitor;

		public MainWindowViewModel(IUiDispatcher uiDispatcher, Window mainWindow, IBulletinMediator bulletinMediator)
		{
			_uiDispatcher = uiDispatcher;
			_uiMonitor = new UiResponsivenessMonitor();

			bulletinMediator.Subscribe<SourceFileOpenedBulletin>(this, x => OnSourceFileChanged(x));

			this.CurrrentFilter = new FilterResultsViewModel(
				mainWindow,
				uiDispatcher,
				bulletinMediator);

			this.CurrentStatus = new MainStatusBarViewModel(
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
			var title = $"Weevil: " + Path.GetFileNameWithoutExtension(bulletin.SourceFilePath);

			_uiDispatcher.Invoke(() => this.ApplicationTitle = title);
		}

		public FilterResultsViewModel CurrrentFilter { get; }

		public MainStatusBarViewModel CurrentStatus { get; }

		public string ApplicationTitle
		{
			get => (string)GetValue(ApplicationTitleProperty);
			private set => SetValue(ApplicationTitleProperty, value);
		}
	}
}
