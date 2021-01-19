namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Media;
	using BlueDotBrigade.Weevil.Data;

	[ValueConversion(typeof(SeverityType), typeof(SolidColorBrush))]
	internal class SeverityToForegroundConverter : IValueConverter
	{
		private static readonly SolidColorBrush DarkGrey = new BrushConverter().ConvertFrom("#666666") as SolidColorBrush;
		private static readonly SolidColorBrush DarkYellow = new BrushConverter().ConvertFrom("#9C6500") as SolidColorBrush;
		private static readonly SolidColorBrush DarkRed = new BrushConverter().ConvertFrom("#900A22") as SolidColorBrush;
		private static readonly SolidColorBrush White = Brushes.WhiteSmoke;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (value != null)
			{
				if (Enum.TryParse<SeverityType>(value.ToString(), out SeverityType severity))
				{
					switch (severity)
					{
						case SeverityType.Verbose:
						case SeverityType.Debug:
							result = DarkGrey;
							break;

						case SeverityType.Warning:
							result = DarkYellow;
							break;

						case SeverityType.Error:
							result = DarkRed;
							break;

						case SeverityType.Critical:
							result = White;
							break;

						default:
							result = Brushes.WhiteSmoke;
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
