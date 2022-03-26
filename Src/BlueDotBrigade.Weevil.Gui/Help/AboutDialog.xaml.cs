namespace BlueDotBrigade.Weevil.Gui.Help
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Windows;
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Input;
	using BlueDotBrigade.Weevil.Gui.Management;
	using BlueDotBrigade.Weevil.IO;
	using File = BlueDotBrigade.Weevil.IO.File;

	/// <summary>
	/// Interaction logic for AboutView.xaml
	/// </summary>
	public partial class AboutDialog : Window
	{
		internal static readonly DependencyProperty DetailsProperty =
			DependencyProperty.Register(
				nameof(Details), typeof(string),
				typeof(AboutDialog));

		internal static readonly DependencyProperty LicenseProperty =
			DependencyProperty.Register(
				nameof(License), typeof(string),
				typeof(AboutDialog));

		private readonly string _thirdPartyNoticesPath;

		public AboutDialog(Version weevilVersion, string licensePath, string thirdPartyNoticesPath, string sourceFilePath)
		{
			var sourceFileSize = GetFileSize(sourceFilePath);
			var computerSnapshot = ComputerSnapshot.Create();

			this.Details =
				$"Weevil: {weevilVersion}" + Environment.NewLine +
				$"Common Language Runtime: {Environment.Version}" + Environment.NewLine +
				$"Operating System: {computerSnapshot.OsName}" + Environment.NewLine +
				$"CPU: {computerSnapshot.CpuName}" + Environment.NewLine +
				$"RAM Free: {computerSnapshot.RamTotalFree.GigaBytes:0.00}GB" + Environment.NewLine;

			if (sourceFileSize.Bytes > 0)
			{
				if (sourceFileSize.MetaBytes < 1)
				{
					this.Details += $"Source File Size: {sourceFileSize.Bytes:#,###,##0} Bytes" + Environment.NewLine;
				}
				else
				{
					this.Details += $"Source File Size: {sourceFileSize.Bytes:#,###,##0} Bytes ({sourceFileSize.MetaBytes:#,###,##0} MB)" + Environment.NewLine;
				}
			}

			this.Details += $"Weevil's core engine is powered by open source software.";

			this.License = new File().ReadAllText(licensePath);

			_thirdPartyNoticesPath = thirdPartyNoticesPath;

			this.DataContext = this;

			InitializeComponent();
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
