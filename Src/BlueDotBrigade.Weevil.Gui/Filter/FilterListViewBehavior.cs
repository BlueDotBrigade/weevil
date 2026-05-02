namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Threading;

	/// <summary>
	/// Provides attached behaviors specific to the filter results <see cref="ListView"/>.
	/// </summary>
	public class FilterListViewBehavior : DependencyObject
	{
		public static int GetActiveRecordIndex(ListView listView)
		{
			return (int)listView.GetValue(ActiveRecordIndexProperty);
		}

		public static void SetActiveRecordIndex(ListView listView, int value)
		{
			listView.SetValue(ActiveRecordIndexProperty, value);
		}

		public static readonly DependencyProperty ActiveRecordIndexProperty =
			DependencyProperty.RegisterAttached(
				"ActiveRecordIndex",
				typeof(int),
				typeof(FilterListViewBehavior),
				new FrameworkPropertyMetadata(
					-1,
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					OnActiveRecordIndexChanged));

		public static bool GetClickSelectsRow(FrameworkElement frameworkElement)
		{
			return (bool)frameworkElement.GetValue(ClickSelectsRowProperty);
		}

		public static void SetClickSelectsRow(FrameworkElement frameworkElement, bool value)
		{
			frameworkElement.SetValue(ClickSelectsRowProperty, value);
		}

		/// <summary>
		/// When enabled on a read-only <see cref="TextBox"/> inside a <see cref="ListViewItem"/>,
		/// ensures that clicking on the text still selects the parent row.
		/// </summary>
		/// <remarks>
		/// A <see cref="TextBox"/> consumes <c>MouseLeftButtonDown</c> internally (to place the text cursor),
		/// which prevents the event from bubbling up to the <see cref="ListViewItem"/> that handles row selection.
		/// This behavior intercepts <c>PreviewMouseLeftButtonDown</c> (tunneling) and re-raises a fresh
		/// <c>MouseLeftButtonDown</c> directly on the parent <see cref="ListViewItem"/>, restoring the
		/// standard WPF selection mechanism — including Ctrl+Click and Shift+Click multi-selection.
		/// </remarks>
		public static readonly DependencyProperty ClickSelectsRowProperty =
			DependencyProperty.RegisterAttached("ClickSelectsRow",
				typeof(bool), typeof(FilterListViewBehavior),
				new FrameworkPropertyMetadata(false, OnClickSelectsRowChanged));

		private static readonly DependencyProperty IsSelectionChangedHandlerAttachedProperty =
			DependencyProperty.RegisterAttached(
				"IsSelectionChangedHandlerAttached",
				typeof(bool),
				typeof(FilterListViewBehavior),
				new PropertyMetadata(false));

		private static readonly DependencyProperty IsUpdatingActiveRecordIndexProperty =
			DependencyProperty.RegisterAttached(
				"IsUpdatingActiveRecordIndex",
				typeof(bool),
				typeof(FilterListViewBehavior),
				new PropertyMetadata(false));

		private static readonly DependencyProperty IsUpdatingSelectionProperty =
			DependencyProperty.RegisterAttached(
				"IsUpdatingSelection",
				typeof(bool),
				typeof(FilterListViewBehavior),
				new PropertyMetadata(false));

		internal static bool HasVisibleItemAtIndex(int activeRecordIndex, int itemCount)
		{
			return activeRecordIndex >= 0 && activeRecordIndex < itemCount;
		}

		internal static bool ShouldNormalizeSelection(
			int selectedItemCount,
			int selectedIndex,
			int activeRecordIndex)
		{
			return selectedItemCount != 1 || selectedIndex != activeRecordIndex;
		}

		private static void OnActiveRecordIndexChanged(
			DependencyObject d,
			DependencyPropertyChangedEventArgs e)
		{
			if (d is not ListView listView || e.NewValue is not int activeRecordIndex)
			{
				return;
			}

			EnsureSelectionChangedHandler(listView);

			if ((bool)listView.GetValue(IsUpdatingActiveRecordIndexProperty) ||
				!HasVisibleItemAtIndex(activeRecordIndex, listView.Items.Count))
			{
				return;
			}

			listView.ScrollIntoView(listView.Items[activeRecordIndex]);
			listView.Dispatcher.BeginInvoke(
				DispatcherPriority.Loaded,
				new Action(() => SynchronizeSelection(listView, activeRecordIndex)));
		}

		private static void OnClickSelectsRowChanged(
			DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is TextBox textBox && e.NewValue is bool enabled)
			{
				if (enabled)
				{
					textBox.PreviewMouseLeftButtonDown += OnTextBoxPreviewMouseLeftButtonDown;
				}
				else
				{
					textBox.PreviewMouseLeftButtonDown -= OnTextBoxPreviewMouseLeftButtonDown;
				}
			}
		}

		private static void EnsureSelectionChangedHandler(ListView listView)
		{
			if ((bool)listView.GetValue(IsSelectionChangedHandlerAttachedProperty))
			{
				return;
			}

			listView.SelectionChanged += OnListViewSelectionChanged;
			listView.SetValue(IsSelectionChangedHandlerAttachedProperty, true);
		}

		private static void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is not ListView listView ||
				(bool)listView.GetValue(IsUpdatingSelectionProperty))
			{
				return;
			}

			listView.SetValue(IsUpdatingActiveRecordIndexProperty, true);
			try
			{
				SetActiveRecordIndex(listView, listView.SelectedIndex);
			}
			finally
			{
				listView.SetValue(IsUpdatingActiveRecordIndexProperty, false);
			}
		}

		private static void SynchronizeSelection(ListView listView, int activeRecordIndex)
		{
			if (!HasVisibleItemAtIndex(activeRecordIndex, listView.Items.Count) ||
				listView.ItemContainerGenerator.ContainerFromIndex(activeRecordIndex) is not ListViewItem item)
			{
				return;
			}

			item.Focus();

			if (!ShouldNormalizeSelection(
				listView.SelectedItems.Count,
				listView.SelectedIndex,
				activeRecordIndex))
			{
				return;
			}

			listView.SetValue(IsUpdatingSelectionProperty, true);
			try
			{
				listView.SelectedItems.Clear();
				listView.SelectedItems.Add(listView.Items[activeRecordIndex]);
			}
			finally
			{
				listView.SetValue(IsUpdatingSelectionProperty, false);
			}
		}

		private static void OnTextBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is TextBox textBox)
			{
				var listViewItem = FindAncestor<ListViewItem>(textBox);
				if (listViewItem != null)
				{
					listViewItem.RaiseEvent(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
					{
						RoutedEvent = UIElement.MouseLeftButtonDownEvent,
					});
				}
			}
		}

		private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
		{
			current = VisualTreeHelper.GetParent(current);
			while (current != null)
			{
				if (current is T found)
				{
					return found;
				}
				current = VisualTreeHelper.GetParent(current);
			}
			return null;
		}
	}
}
