#nullable enable

namespace Weevil.VirtualListPoC;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BlueDotBrigade.Weevil.Gui.Filter;

/// <summary>
/// The grid, and the input it handles itself when the range selection is on.
/// </summary>
/// <remarks>
/// With the range selection, WPF never selects a row: clicks on a row and the navigation keys are handled here
/// and sent to <see cref="MainViewModel"/>, and each row shows its own <c>IsSelected</c>. Clicks on the pin box
/// and the comment box still go to those controls.
/// </remarks>
public partial class MainWindow : Window
{
	private readonly MainViewModel _viewModel;
	private readonly Options _options;
	private ScrollViewer? _scrollViewer;

	internal MainWindow(Options options, MainViewModel viewModel)
	{
		_options = options;
		_viewModel = viewModel;
		InitializeComponent();
		this.DataContext = viewModel;

		if (viewModel.UsesWpfSelection)
		{
			// As Weevil 2.12's FilterView.xaml.
			this.ListView.SelectionMode = SelectionMode.Extended;
			this.ListView.IsSynchronizedWithCurrentItem = true;
			this.ListView.ItemContainerStyle = (Style)FindResource("WpfSelectionRowStyle");
			BindingOperations.SetBinding(
				this.ListView,
				FilterListViewBehavior.ActiveRecordIndexProperty,
				new Binding(nameof(MainViewModel.ActiveRecordIndex)) { Mode = BindingMode.OneWay });
		}
		else
		{
			this.ListView.SelectionMode = SelectionMode.Single;
			this.ListView.IsSynchronizedWithCurrentItem = false;
			this.ListView.ItemContainerStyle = (Style)FindResource("RowStyle");
		}

		viewModel.ScrollRequested += OnScrollRequested;
		viewModel.WpfSelectionRestoreRequested += OnWpfSelectionRestoreRequested;
		this.Loaded += OnLoaded;
		this.Closed += (_, _) => viewModel.Dispose();
	}

	/// <summary>
	/// How many times WPF selected a row on its own while the range selection was on. It should stay at zero.
	/// </summary>
	internal int WpfSelectionsUndone { get; private set; }

	/// <summary>
	/// How long it took to build the results and lay out the first rows.
	/// </summary>
	internal double OpenMilliseconds { get; private set; }

	internal ScrollViewer? ScrollViewer => _scrollViewer;

	internal ListView RowList => this.ListView;

	/// <summary>
	/// Scrolls so that a position is at the top, as dragging the scroll bar does. Offsets count rows, not pixels.
	/// </summary>
	internal void ScrollToPosition(int position)
	{
		_scrollViewer?.ScrollToVerticalOffset(position);
	}

	internal ListViewItem? GetContainer(int position)
	{
		return this.ListView.ItemContainerGenerator.ContainerFromIndex(position) as ListViewItem;
	}

	internal TextBox? FindCommentBox(int position)
	{
		return GetContainer(position) is ListViewItem item ? FindDescendant<TextBox>(item) : null;
	}

	internal static T? FindDescendant<T>(DependencyObject root)
		where T : DependencyObject
	{
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is T match)
			{
				return match;
			}

			T? descendant = FindDescendant<T>(child);
			if (descendant is not null)
			{
				return descendant;
			}
		}

		return null;
	}

	private static bool IsInsideEditor(object? originalSource, ListViewItem item)
	{
		var current = originalSource as DependencyObject;
		while (current is not null && current != item)
		{
			if (current is TextBoxBase || current is ToggleButton)
			{
				return true;
			}

			current = current is Visual || current is System.Windows.Media.Media3D.Visual3D
				? VisualTreeHelper.GetParent(current)
				: LogicalTreeHelper.GetParent(current);
		}

		return false;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		_scrollViewer = FindDescendant<ScrollViewer>(this.ListView);

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		_viewModel.Open();
		this.ListView.UpdateLayout();
		this.OpenMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
		this.ListView.Focus();

		if (_options.RunsAutomatically)
		{
			var runner = new AutoRun(this, _viewModel, _options);
			await runner.RunAsync();
			if (!_options.Stays)
			{
				Close();
			}
		}
	}

	private void OnRowPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (_viewModel.UsesWpfSelection || sender is not ListViewItem item || item.DataContext is not RowViewModel row)
		{
			return;
		}

		if (IsInsideEditor(e.OriginalSource, item))
		{
			return;
		}

		// WPF would select the row; the range selection does it instead.
		e.Handled = true;
		_viewModel.Click(row.Position, Keyboard.Modifiers);
		item.Focus();
	}

	private void OnListViewPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (_viewModel.UsesWpfSelection || _viewModel.IsBusy || e.OriginalSource is TextBoxBase)
		{
			return;
		}

		var count = _viewModel.Rows.Results.Length;
		if (count == 0)
		{
			return;
		}

		ModifierKeys modifiers = Keyboard.Modifiers;
		var control = (modifiers & ModifierKeys.Control) != 0;
		var caret = _viewModel.Caret;
		var page = System.Math.Max(1, (int)(_scrollViewer?.ViewportHeight ?? 20) - 1);

		switch (e.Key)
		{
			case Key.Up:
				_viewModel.MoveCaret(caret - 1, modifiers, Describe("Up", modifiers));
				break;
			case Key.Down:
				_viewModel.MoveCaret(caret + 1, modifiers, Describe("Down", modifiers));
				break;
			case Key.PageUp:
				_viewModel.MoveCaret(caret - page, modifiers, Describe("Page Up", modifiers));
				break;
			case Key.PageDown:
				_viewModel.MoveCaret(caret + page, modifiers, Describe("Page Down", modifiers));
				break;
			case Key.Home:
				_viewModel.MoveCaret(0, modifiers, Describe("Home", modifiers));
				break;
			case Key.End:
				_viewModel.MoveCaret(count - 1, modifiers, Describe("End", modifiers));
				break;
			case Key.A when control:
				_viewModel.SelectAll();
				break;
			case Key.C when control:
				_viewModel.Copy(isInteractive: true);
				break;
			case Key.Space when control:
				_viewModel.ToggleCaret();
				break;
			default:
				return;
		}

		e.Handled = true;
	}

	private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_viewModel.UsesWpfSelection)
		{
			_viewModel.OnWpfSelectionChanged(e.AddedItems, e.RemovedItems);
			return;
		}

		if (this.ListView.SelectedItem is not null)
		{
			// WPF selected a row on its own, for example when a row got focus. The range selection is the only selection.
			this.WpfSelectionsUndone++;
			this.ListView.UnselectAll();
		}
	}

	private void OnScrollRequested(object? sender, int position)
	{
		// WPF finds the row's index with IndexOf, then realizes the rows around it.
		this.ListView.ScrollIntoView(_viewModel.Rows.GetRow(position));
		this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => GetContainer(position)?.Focus()));
	}

	private void OnWpfSelectionRestoreRequested(object? sender, IReadOnlyList<RowViewModel> rows)
	{
		// As Weevil 2.12's FilterView.OnResultsChanged: one Add per remembered record.
		this.ListView.SelectedItems.Clear();
		foreach (RowViewModel row in rows)
		{
			this.ListView.SelectedItems.Add(row);
		}
	}

	private static string Describe(string key, ModifierKeys modifiers)
	{
		return modifiers == ModifierKeys.None ? key : modifiers.ToString().Replace(", ", "+") + "+" + key;
	}
}
