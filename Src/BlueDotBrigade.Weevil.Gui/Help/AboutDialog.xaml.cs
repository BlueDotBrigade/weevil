namespace BlueDotBrigade.Weevil.Gui.Help
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Shapes;
	using BlueDotBrigade.Weevil.Gui.Input;
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
			this.Details =
				$"Weevil: {weevilVersion}" + Environment.NewLine +
				$"Common Language Runtime: {Environment.Version}" + Environment.NewLine +
				$"Operating System: {Environment.OSVersion}" + Environment.NewLine +
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
