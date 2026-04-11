namespace BlueDotBrigade.Weevil.Windows.Controls
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Input;
	using System.Windows.Media;

	/// <revisionHistory>
	///     <revision date="2018/06/20" version="1.0.0" author="Clifford Nelson">
	///  </revisionHistory>
	///  <seealso href="https://www.codeproject.com/info/cpol10.aspx">License: Code Project Open License (CPOL) 1.02</seealso>
	public class TextBoxBehavior
	{
		public static bool GetAutoSelectEnabled(FrameworkElement frameworkElement)
		{
			return (bool)frameworkElement.GetValue(AutoSelectEnabledProperty);
		}

		public static void SetAutoSelectEnabled(FrameworkElement frameworkElement, bool value)
		{
			frameworkElement.SetValue(AutoSelectEnabledProperty, value);
		}

		/// <summary>
		/// Enables text box content to be automatically selected when the control receives focus.
		/// </summary>
		public static readonly DependencyProperty AutoSelectEnabledProperty =
			DependencyProperty.RegisterAttached("AutoSelectEnabled",
				typeof(bool), typeof(TextBoxBehavior),
				new FrameworkPropertyMetadata(false, OnAutoSelectEnabledChanged));

		private static void OnAutoSelectEnabledChanged
			(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var frameworkElement = d as FrameworkElement;
			if (frameworkElement == null)
			{
				return;
			}

			if (e.NewValue is bool == false)
			{
				return;
			}

			if ((bool)e.NewValue)
			{
				frameworkElement.GotFocus += SelectAll;
				frameworkElement.PreviewMouseDown += IgnoreMouseButton;
			}
			else
			{
				frameworkElement.GotFocus -= SelectAll;
				frameworkElement.PreviewMouseDown -= IgnoreMouseButton;
			}
		}

		private static void SelectAll(object sender, RoutedEventArgs e)
		{
			var frameworkElement = e.OriginalSource as FrameworkElement;
			if (frameworkElement is TextBox)
			{
				((TextBoxBase)frameworkElement).SelectAll();
			}
			else if (frameworkElement is PasswordBox)
			{
				((PasswordBox)frameworkElement).SelectAll();
			}
		}

		private static void IgnoreMouseButton
			(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var frameworkElement = sender as FrameworkElement;
			if (frameworkElement == null || frameworkElement.IsKeyboardFocusWithin)
			{
				return;
			}

			e.Handled = true;
			frameworkElement.Focus();
		}

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
				typeof(bool), typeof(TextBoxBehavior),
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
