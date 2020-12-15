namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Media;
	using BlueDotBrigade.Weevil.Data;

	[ValueConversion(typeof(SeverityType), typeof(SolidColorBrush))]
	public class SeverityToBackgroundConverter : IValueConverter
	{
		private static readonly SolidColorBrush LightYellow = new BrushConverter().ConvertFrom("#fff5cf") as SolidColorBrush;
		private static readonly SolidColorBrush LightRed = new BrushConverter().ConvertFrom("#ffe1e4") as SolidColorBrush;
		private static readonly SolidColorBrush DarkRed = new BrushConverter().ConvertFrom("#9C0006") as SolidColorBrush;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (value != null)
			{
				if (Enum.TryParse<SeverityType>(value.ToString(), out SeverityType severity))
				{
					switch (severity)
					{
						case SeverityType.Warning:
							result = LightYellow;
							break;

						case SeverityType.Error:
							result = LightRed;
							break;

						case SeverityType.Critical:
							result = DarkRed;
							break;

						default:
							result = Brushes.Transparent;
							break;
					}
				}
			}

			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
