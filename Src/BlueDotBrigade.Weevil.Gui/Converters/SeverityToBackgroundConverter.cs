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
							result = ResolveBrush("SeverityWarningBackgroundBrush");
							break;

						case SeverityType.Error:
							result = ResolveBrush("SeverityErrorBackgroundBrush");
							break;

						case SeverityType.Critical:
							result = ResolveBrush("SeverityCriticalBackgroundBrush");
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

		private static object ResolveBrush(string resourceKey)
		{
			return Application.Current?.TryFindResource(resourceKey) as SolidColorBrush ?? DependencyProperty.UnsetValue;
		}
	}
}
