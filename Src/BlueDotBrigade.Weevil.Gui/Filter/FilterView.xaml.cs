namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Data.Common;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Properties;
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
					var viewModel = args.OldValue as FilterViewModel;

					viewModel.ResultsChanged -= OnResultsChanged;
					
				}
				if (args.NewValue != null)
				{
					var viewModel = args.NewValue as FilterViewModel;

					viewModel.ResultsChanged += OnResultsChanged;
				}
			};

			InitializeComponent();

			Loaded += OnControlLoaded;
		}

		private void OnControlLoaded(object sender, System.Windows.RoutedEventArgs e)
		{
			var window = Window.GetWindow(this);
			window.Closing += OnWindowClosing;

			ApplicationFontSizeComboBox.SelectedValue = Settings.Default.ApplicationFontSize;
			RowFontSizeSlider.Value = Settings.Default.RowFontSize;
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
				this.ListView.SelectedItems.Clear();

				foreach (IRecord record in this.ViewModel.SelectedItems)
				{
					this.ListView.SelectedItems.Add(record);
				}

				// Force bindings (and converters) for only the visible items to be refreshed.
				this.ListView.Items.Refresh();
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

		private void UpdateLayout()
		{
			base.UpdateLayout();

			if (ListView.View is GridView gridView)
			{
				// Re-size columns based on the application's font size				
				for (var i = 0; i < gridView.Columns.Count; i++)
				{
					GridViewColumn column = gridView.Columns[i];

					// Set the width to Auto and then back to its previous value to force recalculation
					var originalWidth = column.Width;
					column.Width = 0; // Setting to 0 first to force recalculation
					column.Width = originalWidth;
				}

				ListView.UpdateLayout();

				var totalWidth = 0.0;

				// Calculate the width of the last column
				for (var i = 0; i < gridView.Columns.Count; i++)
				{
					GridViewColumn column = gridView.Columns[i];

					if (i == gridView.Columns.Count - 1)
					{
						var remainingWidth = Math.Max(0, ListView.ActualWidth - SystemParameters.VerticalScrollBarWidth - totalWidth - 2);
						column.Width = 0; // Setting to 0 first to force recalculation
						column.Width = remainingWidth;
					}
					else
					{
						totalWidth += column.ActualWidth;
					}
				}
			}
		}

		private void ApplicationFontSizeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			// If the user control has not yet been created, early exit to avoid saving WPF default values to settings (#251).
			if (!this.IsLoaded) return;

			if (ApplicationFontSizeComboBox.SelectedValue is string selectedValue &&
				double.TryParse(selectedValue, out double fontSize))
			{
				Application.Current.Resources["ApplicationFontSize"] = fontSize;

				UpdateLayout();
			}
		}

		private void RowFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			// If the user control has not yet been created, early exit to avoid saving WPF default values to settings (#251).
			if (!this.IsLoaded) return;

			Application.Current.Resources["RowFontSize"] = e.NewValue;
			Settings.Default.RowFontSize = e.NewValue;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			UpdateLayout();
		}
	}
}