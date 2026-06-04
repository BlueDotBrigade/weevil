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
							result = ResolveBrush("SeverityWarningBackgroundBrush", FallbackSeverityBrush);
							break;

						case SeverityType.Error:
							result = ResolveBrush("SeverityErrorBackgroundBrush", FallbackSeverityBrush);
							break;

						case SeverityType.Critical:
							result = ResolveBrush("SeverityCriticalBackgroundBrush", FallbackSeverityBrush);
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

		private static object ResolveBrush(string resourceKey, SolidColorBrush fallback)
		{
			return Application.Current?.TryFindResource(resourceKey) as SolidColorBrush ?? fallback ?? DependencyProperty.UnsetValue;
		}
	}
}
