namespace BlueDotBrigade.Weevil.Gui.Help
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Timers;
	using System.Windows;
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Input;
	using BlueDotBrigade.Weevil.Gui.Management;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.IO;
	using File = BlueDotBrigade.Weevil.IO.File;

	/// <summary>
	/// Interaction logic for AboutView.xaml
	/// </summary>
	public partial class AboutDialog : Window
	{
		private static readonly TimeSpan DefaultTimerPeriod = TimeSpan.FromMilliseconds(200);

		internal static readonly DependencyProperty DetailsProperty =
			DependencyProperty.Register(
				nameof(Details), typeof(string),
				typeof(AboutDialog));

		internal static readonly DependencyProperty LicenseProperty =
			DependencyProperty.Register(
				nameof(License), typeof(string),
				typeof(AboutDialog));

		private readonly IUiDispatcher _uiDispatcher;
		private readonly Version _weevilVersion;
		private readonly string _thirdPartyNoticesPath;
		private readonly string _sourceFilePath;
		private readonly Timer _timer;

		internal AboutDialog(
			IUiDispatcher uiDispatcher,
			Version weevilVersion, 
			string licensePath, 
			string thirdPartyNoticesPath, 
			string sourceFilePath)
		{
			_uiDispatcher = uiDispatcher;
			_weevilVersion = weevilVersion;
			_thirdPartyNoticesPath = thirdPartyNoticesPath;
			_sourceFilePath = sourceFilePath;

			this.Details = GetHeader(_weevilVersion) + Environment.NewLine +
			    Environment.NewLine +
				"Loading metrics...";

			this.License = new File().ReadAllText(licensePath);
			this.DataContext = this;

			InitializeComponent();

			_timer = new Timer
			{
				Interval = DefaultTimerPeriod.TotalMilliseconds,
				AutoReset = false,
			};
			_timer.Elapsed += OnTimerElapsed;
			_timer.Start();
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			_uiDispatcher.Invoke(() =>
			{
				this.Details = GetHeader(_weevilVersion) + Environment.NewLine +
				               GetMetrics(_sourceFilePath);
			});
		}

		internal string Details
		{
			get => (string)GetValue(DetailsProperty);
			set => SetValue(DetailsProperty, value);
		}

		internal string License
		{
			get => (string)GetValue(LicenseProperty);
			set => SetValue(LicenseProperty, value);
		}

		private static string GetHeader(Version weevilVersion)
		{
			return
				$"Weevil: {weevilVersion}" + Environment.NewLine +
				$"Weevil's core engine is powered by open source software." +
				Environment.NewLine;
		}

		private static string GetMetrics(string sourceFilePath)
		{
			string result = string.Empty;

			var sourceFileSize = GetFileSize(sourceFilePath);
			var computerSnapshot = ComputerSnapshot.Create();
			var process = Process.GetCurrentProcess();
			var weevilRamUsed = TaskManager.GetPrivateWorkingSet(process);
			var totalRamAvailable = weevilRamUsed + computerSnapshot.RamTotalFree;
			var percentUsage = ((float)weevilRamUsed.Bytes / (float)totalRamAvailable.Bytes) * 100.0;

			result +=
				$"Common Language Runtime: {Environment.Version}" + Environment.NewLine +
				$"Operating System: {computerSnapshot.OsName}" + Environment.NewLine +
				$"CPU: {computerSnapshot.CpuName}" + Environment.NewLine +
				Environment.NewLine +
				$"RAM Installed: {computerSnapshot.RamTotalInstalled.GigaBytes:0.0} GB" + Environment.NewLine +
				$"RAM Available: {computerSnapshot.RamTotalFree.GigaBytes:0.0} GB" + Environment.NewLine +
				$"RAM Used {weevilRamUsed.MetaBytes:#,###,##0} MB ({percentUsage:0.0} %) as reported by Task Manager" + Environment.NewLine +
				Environment.NewLine;

			if (sourceFileSize.Bytes > 0)
			{
				if (sourceFileSize.MetaBytes < 1)
				{
					result += $"Source File Size: {sourceFileSize.Bytes:#,###,##0} Bytes" + Environment.NewLine;
				}
				else
				{
					result += $"Source File Size: {sourceFileSize.MetaBytes:#,###,##0} MB" + Environment.NewLine;
				}
			}

			return result;
		}

		private static StorageUnit GetFileSize(string sourceFilePath)
		{
			var fileSize = StorageUnit.Zero;

			try
			{
				if (!string.IsNullOrEmpty(sourceFilePath))
				{
					if (new File().Exists(sourceFilePath))
					{
						fileSize = new StorageUnit(new FileInfo(sourceFilePath).Length);
					}
				}
			}
			catch (Exception e)
			{
				Log.Default.Write(
					LogSeverityType.Error,
					e,
					"Source file size could not be determined.");
			}

			return fileSize;
		}

		public ICommand ShowThirdPartyNoticesCommand => new UiBoundCommand(() => Process.Start(_thirdPartyNoticesPath));
	}
}
