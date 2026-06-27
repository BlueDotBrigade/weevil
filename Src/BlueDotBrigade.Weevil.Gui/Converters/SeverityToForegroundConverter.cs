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
		public SolidColorBrush WarningBrush { get; set; }
		public SolidColorBrush ErrorBrush { get; set; }
		public SolidColorBrush CriticalBrush { get; set; }

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
							result = this.WarningBrush ?? FallbackSeverityBrush;
							break;

						case SeverityType.Error:
							result = this.ErrorBrush ?? FallbackSeverityBrush;
							break;

						case SeverityType.Critical:
							result = this.CriticalBrush ?? FallbackSeverityBrush;
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
