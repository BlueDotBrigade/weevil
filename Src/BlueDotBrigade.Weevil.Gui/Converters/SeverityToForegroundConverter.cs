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
		private static readonly SolidColorBrush FallbackSeverityBrush = Brushes.Transparent;

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
							result = Application.Current?.TryFindResource("SeverityWarningForegroundBrush") as SolidColorBrush ?? FallbackSeverityBrush ?? DependencyProperty.UnsetValue;
							break;

						case SeverityType.Error:
							result = Application.Current?.TryFindResource("SeverityErrorForegroundBrush") as SolidColorBrush ?? FallbackSeverityBrush ?? DependencyProperty.UnsetValue;
							break;

						case SeverityType.Critical:
							result = Application.Current?.TryFindResource("SeverityCriticalForegroundBrush") as SolidColorBrush ?? FallbackSeverityBrush ?? DependencyProperty.UnsetValue;
							break;

						default:
							result = DependencyProperty.UnsetValue;
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
