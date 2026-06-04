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
							result = Application.Current?.TryFindResource("SeverityWarningBackgroundBrush") as SolidColorBrush ?? FallbackSeverityBrush ?? DependencyProperty.UnsetValue;
							break;

						case SeverityType.Error:
							result = Application.Current?.TryFindResource("SeverityErrorBackgroundBrush") as SolidColorBrush ?? FallbackSeverityBrush ?? DependencyProperty.UnsetValue;
							break;

						case SeverityType.Critical:
							result = Application.Current?.TryFindResource("SeverityCriticalBackgroundBrush") as SolidColorBrush ?? FallbackSeverityBrush ?? DependencyProperty.UnsetValue;
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
