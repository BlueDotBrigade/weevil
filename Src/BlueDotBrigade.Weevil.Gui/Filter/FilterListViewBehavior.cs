namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;

	/// <summary>
	/// Provides attached behaviors specific to the filter results <see cref="ListView"/>.
	/// </summary>
	public class FilterListViewBehavior
	{
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
