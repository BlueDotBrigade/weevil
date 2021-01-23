namespace BlueDotBrigade.Weevil.Windows.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	[ValueConversion(typeof(Version), typeof(string))]
	public class VersionConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (value != null)
			{
				if (value is Version version)
				{
					result = version.ToString();
				}
			}

			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (value != null)
			{
				if (value is string stringValue)
				{
					if (Version.TryParse(stringValue, out Version versionValue))
					{
						result = versionValue;
					}
				}
			}

			return result;
		}
	}
}
