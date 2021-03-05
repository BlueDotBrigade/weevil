namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	// This class is being used as a quick hack to address an unexpected validation error.
	// For more information, see: https://github.com/BlueDotBrigade/weevil/issues/58
	class CheckBoxValidationErrorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}

		public object ConvertBack(object value, Type targetType,  object parameter, CultureInfo culture)
		{
			var result = false;

			if (value is bool booleanValue)
			{
				result = booleanValue;
			}

			return result;
		}
	}
}
