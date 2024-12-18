﻿namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.Navigation;

	/// <summary>
	/// Interaction logic for FilterResultsView.xaml
	/// </summary>
	public partial class FilterView : UserControl
	{
		public FilterView()
		{
			DataContextChanged += (sender, args) =>
			{
				if (args.OldValue != null)
				{
					((FilterViewModel)args.OldValue).ResultsChanged -= OnResultsChanged;
				}
				if (args.NewValue != null)
				{
					((FilterViewModel)args.NewValue).ResultsChanged += OnResultsChanged;
				}
			};

			//var uiDispatcher = new UiDispatcher(Application.Current.Dispatcher);
			//this.DataContext = new FilterResultsViewModel(Application.Current.MainWindow, uiDispatcher);

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

		private FilterViewModel ViewModel => (FilterViewModel)this.DataContext;

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
				this.ListView.SelectedItems.Clear();

				foreach (IRecord record in this.ViewModel.SelectedItems)
				{
					this.ListView.SelectedItems.Add(record);
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
			var added = e.AddedItems.Cast<IRecord>().ToList();
			if (added.Count > 0)
			{
				try
				{
					this.ViewModel.Select(added);
				}
				catch (RecordNotFoundException recordException)
				{
					var message = "Attempting to select a record that is no longer visible.";

					Log.Default.Write(
						LogSeverityType.Error,
						recordException,
						message);
				}
			}

			var removed = e.RemovedItems.Cast<IRecord>().ToList();
			if (removed.Count > 0)
			{
				try
				{
					this.ViewModel.UnSelect(removed);
				}
				catch (RecordNotFoundException recordException)
				{
					var message = "Attempting to de-select a record that is no longer visible.";

					Log.Default.Write(
						LogSeverityType.Error,
						recordException,
						message);
					throw;
				}
			}
		}

		private void OnGotFocus(object sender, RoutedEventArgs e)
		{
			if (sender == this.InclusiveFilter)
			{
				IncludeColumn.Width = new GridLength(3, GridUnitType.Star);
				ExcludeColumn.Width = new GridLength(1, GridUnitType.Star);
			}
			else if (sender == this.ExclusiveFilter)
			{
				IncludeColumn.Width = new GridLength(1, GridUnitType.Star);
				ExcludeColumn.Width = new GridLength(3, GridUnitType.Star);
			}
		}
	}
}