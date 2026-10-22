// Proof of concept for #1014: can a WPF ListView show 100 million rows, using Weevil's row template,
// when a row view-model exists only while it is on screen and the selection is stored as ranges?
// Standalone: it does not reference Weevil. WeevilCopies/ holds verbatim copies of the files it needs.

#nullable enable

namespace Weevil.VirtualListPoC;

using System.Windows;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		Options options;
		try
		{
			options = Options.Parse(e.Args);
		}
		catch (ArgumentException exception)
		{
			MessageBox.Show(exception.Message + "\n\n" + Options.Help, "Weevil.VirtualListPoC", MessageBoxButton.OK, MessageBoxImage.Error);
			Shutdown(2);
			return;
		}

		var viewModel = new MainViewModel(options);
		var window = new MainWindow(options, viewModel);
		this.MainWindow = window;
		window.Show();
	}
}
