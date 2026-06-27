namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	[ValueConversion(typeof(string), typeof(bool))]
	public sealed class StringToBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null || parameter is null)
			{
				return false;
			}

			var valueString = value.ToString();
			var parameterString = parameter.ToString();

			return string.Equals(valueString, parameterString, StringComparison.OrdinalIgnoreCase);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not bool isChecked || !isChecked)
			{
				return Binding.DoNothing;
			}

			if (parameter is null)
			{
				return Binding.DoNothing;
			}

			return parameter.ToString();
		}
	}
}
