namespace BlueDotBrigade.Weevil.Gui
{
	using System.ComponentModel;
	using System.Windows;

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly MainWindowViewModel _viewModel;

		public MainWindow()
		{
			InitializeComponent();

			_viewModel = new MainWindowViewModel();
			this.DataContext = _viewModel;

			base.Loaded += OnMainWindowLoaded;
			base.Closing += OnMainWindowClosing;
		}

		private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
		{
			// We start monitoring after the window is loaded
			// ... thus avoiding the false positive that would occur
			// ... when the application first starts.
			_viewModel.Start();
		}

		private void OnMainWindowClosing(object sender, CancelEventArgs e)
		{
			_viewModel.Stop();
		}
	}
}
