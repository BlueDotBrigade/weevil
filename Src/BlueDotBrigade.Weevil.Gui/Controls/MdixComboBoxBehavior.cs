namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;

	/// <summary>
	/// Enables the MDIX ComboBox to behave more like a Microsoft ComboBox,
	/// so that a list of options is only displayed when the user clicks the down arrow.
	/// </summary>
	/// <seealso href="https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/master/MaterialDesignThemes.Wpf/Themes/MaterialDesignTheme.ComboBox.xaml">MDIX ComboBox</seealso>
	/// <seealso href="https://stackoverflow.com/q/66339015/949681">StackOverflow: HowTo: Change “Material Design In Xaml” ComboBox behavior to only display dropdown when you click the arrow?</seealso>
	internal static class MdixComboBoxBehavior
	{
		public static readonly DependencyProperty UseMicrosoftBehaviorProperty = DependencyProperty.RegisterAttached(
			"UseMicrosoftBehavior", 
			typeof(bool), 
			typeof(MdixComboBoxBehavior), 
			new UIPropertyMetadata(default(bool), UseMicrosoftBehaviorChanged));

		public static bool GetUseMicrosoftBehavior(DependencyObject obj)
		{
			return (bool)obj.GetValue(UseMicrosoftBehaviorProperty);
		}

		public static void SetUseMicrosoftBehavior(DependencyObject obj, bool value)
		{
			obj.SetValue(UseMicrosoftBehaviorProperty, value);
		}

		public static void UseMicrosoftBehaviorChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			if (s is ComboBox comboBox)
			{
				comboBox.Loaded += OnControlLoaded;
			}
		}

		private static void OnControlLoaded(object sender, RoutedEventArgs e)
		{
			if (sender is ComboBox comboBox)
			{
				if (comboBox.Template != null)
				{
					if (comboBox.Template.FindName("toggleButton", comboBox) is ToggleButton button)
					{
						// ComboBox will now only display a list of options,
						// when the user clicks the down arrow button.
						button.BorderThickness = new Thickness(1);
						button.Width = 10;
						button.HorizontalAlignment = HorizontalAlignment.Right;
					}
				}
			}
		}
	}
}
