namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Data.Common;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Controls;
	using BlueDotBrigade.Weevil.Gui.Properties;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.Navigation;

	/// <summary>
	/// Interaction logic for FilterResultsView.xaml
	/// </summary>
	public partial class FilterView : UserControl
	{
		private bool _isLogFileOpening;
		private bool _isCloseDeferred;
		private bool _hadFocusWhenHidden;
		private ScrollViewer _scrollViewer;

		public FilterView()
		{
			DataContextChanged += (sender, args) =>
			{
				if (args.OldValue != null)
				{
					var viewModel = args.OldValue as FilterViewModel;

					viewModel.FileOpened -= OnFileOpened;
					viewModel.ResultsChanged -= OnResultsChanged;
					viewModel.RegionsChanged -= OnRegionsChanged;
					viewModel.BookmarksChanged -= OnBookmarksChanged;
					viewModel.ScrollRequested -= OnScrollRequested;
				}
				if (args.NewValue != null)
				{
					var viewModel = args.NewValue as FilterViewModel;

					viewModel.FileOpened += OnFileOpened;
					viewModel.ResultsChanged += OnResultsChanged;
					viewModel.RegionsChanged += OnRegionsChanged;
					viewModel.BookmarksChanged += OnBookmarksChanged;
					viewModel.ScrollRequested += OnScrollRequested;
				}
			};

			InitializeComponent();

			Loaded += OnControlLoaded;
		}


		private FilterViewModel ViewModel => (FilterViewModel)this.DataContext;

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
				Task analysis = this.ViewModel.AnalysisTask;

				if (!analysis.IsCompleted)
				{
					// Closing now would skip saving the sidecar, so close once the analysis has finished (#996).
					// Do not wait for it here: the analysis needs the UI thread for its prompts and to finish.
					e.Cancel = true;

					if (!_isCloseDeferred)
					{
						_isCloseDeferred = true;

						var window = (Window)sender;
						analysis.ContinueWith(
							_ =>
							{
								_isCloseDeferred = false;
								window.Close();
							},
							TaskScheduler.FromCurrentSynchronizationContext());
					}
				}
				else if (this.ViewModel.ExitCommand.CanExecute(null))
				{
					this.ViewModel.ExitCommand.Execute(null);
				}
			}
		}

		private void OnFileOpened(object sender, EventArgs e)
		{
			_isLogFileOpening = true;
		}

		private void OnRegionsChanged(object sender, EventArgs e)
		{
			RefreshRows();
		}

		private void OnBookmarksChanged(object sender, EventArgs e)
		{
			RefreshRows();
		}

		/// <summary>
		/// Refreshes the bindings and converters of the rows on screen.
		/// </summary>
		/// <remarks>
		/// Refreshing replaces the rows' containers, so the focused row loses focus: it is given back.
		/// </remarks>
		private void RefreshRows()
		{
			var hadFocus = this.ListView.IsKeyboardFocusWithin;

			this.ListView.Items.Refresh();

			if (hadFocus)
			{
				FocusRow(this.ViewModel.CaretPosition);
			}
		}

		private void OnResultsChanged(object sender, EventArgs e)
		{
			// Keep the caret's row in view, as Weevil 2.12 did, even when the filter started from the filter box.
			ScrollToRow(this.ViewModel?.CaretPosition ?? -1);

			// Recalculate column widths after filter results change.
			// Note: BeginInvoke at Background priority ensures that this runs after
			// any pending progress bar hide and rendering operations are complete.
			this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(UpdateLayout));
		}

		private void OnListViewRowPressed(object sender, RowPressedEventArgs e)
		{
			if (e.Item is RowViewModel row)
			{
				this.ViewModel?.PressRow(row, e.Button == MouseButton.Right, e.Modifiers);
			}
		}

		private void OnListViewPreviewKeyDown(object sender, KeyEventArgs e)
		{
			// Keys typed in the comment box belong to it.
			if (this.ViewModel == null || e.OriginalSource is TextBoxBase)
			{
				return;
			}

			if (this.ViewModel.PressKey(e.Key, Keyboard.Modifiers, GetPageSize(), isOnRow: e.OriginalSource is ListViewItem))
			{
				e.Handled = true;
			}
		}

		/// <remarks>
		/// The caret follows keyboard focus into a row, for example when its pin box is clicked, so the keys move from there.
		/// </remarks>
		private void OnListViewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (ItemsControl.ContainerFromElement(this.ListView, e.NewFocus as DependencyObject) is ListViewItem { DataContext: RowViewModel row })
			{
				this.ViewModel?.MoveCaretTo(row);
			}
		}

		/// <remarks>
		/// The grid is hidden while a filter runs, which takes keyboard focus away: it is given back to the caret's row.
		/// </remarks>
		private void OnListViewIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (!(bool)e.NewValue)
			{
				_hadFocusWhenHidden = this.ListView.IsKeyboardFocusWithin;
			}
			else if (_hadFocusWhenHidden && this.ViewModel != null)
			{
				_hadFocusWhenHidden = false;

				// Hiding the grid moved focus to an ancestor. Give it back only if the user has not moved it elsewhere meanwhile.
				if (Keyboard.FocusedElement is not Visual focused || focused.IsAncestorOf(this.ListView) || this.ListView.IsAncestorOf(focused))
				{
					FocusRow(this.ViewModel.CaretPosition);
				}
			}
		}

		private void OnScrollRequested(object sender, int position)
		{
			FocusRow(position);
		}

		/// <summary>
		/// Scrolls the row at <paramref name="position"/> into view, then gives it keyboard focus (#699).
		/// </summary>
		private void FocusRow(int position)
		{
			if (ScrollToRow(position))
			{
				this.Dispatcher.BeginInvoke(
					DispatcherPriority.Loaded,
					new Action(() => this.ListView.ContainerFromPosition(position)?.Focus()));
			}
		}

		/// <returns><see langword="false"/> when there is no row at <paramref name="position"/>.</returns>
		private bool ScrollToRow(int position)
		{
			RowList rows = this.ViewModel?.Rows;

			if (rows == null || position < 0 || position >= rows.Count)
			{
				return false;
			}

			this.ListView.ScrollIntoView(rows.GetRow(position));
			return true;
		}

		/// <summary>
		/// Returns the number of rows Page Up and Page Down move: one less than the rows shown.
		/// </summary>
		private int GetPageSize()
		{
			_scrollViewer ??= FindDescendant<ScrollViewer>(this.ListView);

			// The grid scrolls by row, so the viewport's height is a number of rows.
			return System.Math.Max(1, (int)(_scrollViewer?.ViewportHeight ?? 20) - 1);
		}

		private static T FindDescendant<T>(DependencyObject parent)
			where T : DependencyObject
		{
			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);
				T found = child as T ?? FindDescendant<T>(child);

				if (found != null)
				{
					return found;
				}
			}

			return null;
		}

		private void OnWindowLayoutDragOver(object sender, DragEventArgs e)
		{
			if (this.ViewModel != null)
			{
				this.ViewModel.DragOver(e);
			}
		}

		private void OnWindowLayoutDrop(object sender, DragEventArgs e)
		{
			if (this.ViewModel != null)
			{
				this.ViewModel.Drop(e);
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

		/// <summary>
		/// Adjusts the position and size of user controls, thus ensuring that they have been 
		/// arranged correctly based on the available space and layout rules.
		/// </summary>
		/// <remarks>
		/// Layout is influenced by a number of factors:
		/// <list type="bullet">
		///		<item>font size</item>
		///		<item>column width</item>
		///		<item>only applies to visible controls</item>
		///		<item>etc.</item>
		/// </list>
		/// </remarks>
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
						var remainingWidth = System.Math.Max(0, ListView.ActualWidth - SystemParameters.VerticalScrollBarWidth - totalWidth - 2);
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

		private void OnProgressBarVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (_isLogFileOpening)
			{
				_isLogFileOpening = false;

				// The following ensures that the filter results are displayed properly.
				// NOTE: The ListView layout can only be adjusted when the control is visible (i.e. progress bar hidden).
				UpdateLayout();
			}
		}
	}
}
