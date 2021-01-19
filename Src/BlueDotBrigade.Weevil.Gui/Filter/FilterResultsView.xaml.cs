namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;

	/// <summary>
	/// Interaction logic for FilterResultsView.xaml
	/// </summary>
	public partial class FilterResultsView : UserControl
	{
		public FilterResultsView()
		{
			DataContextChanged += (sender, args) =>
			{
				if (args.OldValue != null)
				{
					((FilterResultsViewModel)args.OldValue).ResultsChanged -= OnResultsChanged;
				}
				if (args.NewValue != null)
				{
					((FilterResultsViewModel)args.NewValue).ResultsChanged += OnResultsChanged;
				}
			};

			this.DataContext = new FilterResultsViewModel(Application.Current.MainWindow, Application.Current.Dispatcher);

			InitializeComponent();

			Loaded += OnControlLoaded;
		}

		private void OnControlLoaded(object sender, System.Windows.RoutedEventArgs e)
		{
			var window = Window.GetWindow(this);
			window.Closing += OnWindowClosing;
		}

		private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (this.ViewModel != null)
			{
				if (this.ViewModel.ExitCommand.CanExecute(null))
				{
					this.ViewModel.ExitCommand.Execute(null);
				}
			}
		}

		private FilterResultsViewModel ViewModel => (FilterResultsViewModel)this.DataContext;

		private void OnResultsChanged(object sender, EventArgs e)
		{
			try
			{
				// PROBLEM: 
				// Log files are quite large, which means there is the potential for a log of `PropertyChanged` events
				// firing as records are removed/hidden during the filtering process.
				// 
				// SOLUTION:
				// Rather than fire thousands of `PropertyChanged` events, one alternative is to simply bind the ListView control
				// to the new results when the are available.  This in itself creates some challenges:
				// - now we have to have code-behind that directly manipulates the `View`
				// - it is harder to create an automated test for the `ViewModel` to ensure selected records feature is working
				//ListView.SelectedItems.Clear();

				foreach (IRecord record in this.ViewModel.SelectedItems)
				{
					//ListView.SelectedItems.Add(record);
				}
			}
			catch (Exception exception)
			{
				var message =
					"An unexpected error occurred while updating the selected items after the filter changed.";

				Log.Default.Write(
					LogSeverityType.Error,
					exception,
					message);
				MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.ViewModel.Select(e.AddedItems.Cast<IRecord>().ToList());
			this.ViewModel.UnSelect(e.RemovedItems.Cast<IRecord>().ToList());
		}
	}
}
