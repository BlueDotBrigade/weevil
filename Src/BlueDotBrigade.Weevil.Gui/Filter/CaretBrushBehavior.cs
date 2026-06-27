namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	public sealed class CaretBrushBehavior
	{
		public static readonly DependencyProperty CaretBrushProperty = DependencyProperty.RegisterAttached(
			"CaretBrush",
			typeof(Brush),
			typeof(CaretBrushBehavior),
			new UIPropertyMetadata(default(Brush), OnCaretBrushChanged));

		public static Brush GetCaretBrush(DependencyObject obj)
		{
			return (Brush)obj.GetValue(CaretBrushProperty);
		}

		public static void SetCaretBrush(DependencyObject obj, Brush value)
		{
			obj.SetValue(CaretBrushProperty, value);
		}

		private static void OnCaretBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is TextBox textBox)
			{
				ApplyCaretBrush(textBox, e.NewValue as Brush);
				return;
			}

			if (d is ComboBox comboBox)
			{
				comboBox.Loaded -= OnComboBoxLoaded;
				comboBox.Loaded += OnComboBoxLoaded;

				if (comboBox.IsLoaded)
				{
					ApplyToComboBox(comboBox);
				}
			}
		}

		private static void OnComboBoxLoaded(object sender, RoutedEventArgs e)
		{
			if (sender is ComboBox comboBox)
			{
				ApplyToComboBox(comboBox);
			}
		}

		private static void ApplyToComboBox(ComboBox comboBox)
		{
			comboBox.ApplyTemplate();

			if (comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
			{
				ApplyCaretBrush(textBox, GetCaretBrush(comboBox));
			}
		}

		private static void ApplyCaretBrush(TextBox textBox, Brush brush)
		{
			if (brush == null)
			{
				textBox.ClearValue(TextBox.CaretBrushProperty);
				return;
			}

			textBox.CaretBrush = brush;
		}
	}
}
