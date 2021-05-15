namespace BlueDotBrigade.Weevil.Gui.Help
{
	using System;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Gui.Input;
	using BlueDotBrigade.Weevil.Gui.Management;
	using BlueDotBrigade.Weevil.IO;

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

		public AboutDialog(Version weevilVersion, string licensePath, string thirdPartyNoticesPath)
		{
			var computerSnapshot = ComputerSnapshot.Create();

			this.Details =
				$"Weevil: {weevilVersion}" + Environment.NewLine +
				$"Common Language Runtime: {Environment.Version}" + Environment.NewLine +
				$"Operating System: {computerSnapshot.OsName}" + Environment.NewLine +
				$"CPU: {computerSnapshot.CpuName}" + Environment.NewLine +
				$"RAM Free: {computerSnapshot.RamTotalFree.GigaBytes:0.00}GB" + Environment.NewLine +
				Environment.NewLine +
				$"Open source software powered by Blue Dot Brigade.";

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

		public ICommand ShowThirdPartyNoticesCommand => new UiBoundCommand(() => Process.Start(_thirdPartyNoticesPath));
	}
}
