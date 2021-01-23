namespace BlueDotBrigade.Weevil.Windows.Controls
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;

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
	}
}
