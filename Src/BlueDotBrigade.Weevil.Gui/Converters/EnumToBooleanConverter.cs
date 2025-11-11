namespace BlueDotBrigade.Weevil.Gui.Converters
{
        using System;
        using System.Globalization;
        using System.Windows.Data;

        [ValueConversion(typeof(Enum), typeof(bool))]
        public sealed class EnumToBooleanConverter : IValueConverter
        {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                        if (value is null || parameter is null)
                        {
                                return false;
                        }

                        var parameterString = parameter.ToString();
                        if (string.IsNullOrWhiteSpace(parameterString))
                        {
                                return false;
                        }

                        var valueType = value.GetType();

                        if (!valueType.IsEnum)
                        {
                                return false;
                        }

                        return Enum.TryParse(valueType, parameterString, true, out var parameterValue)
                                && parameterValue.Equals(value);
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

                        var parameterString = parameter.ToString();
                        if (string.IsNullOrWhiteSpace(parameterString))
                        {
                                return Binding.DoNothing;
                        }

                        var enumType = targetType;
                        if (!enumType.IsEnum)
                        {
                                return Binding.DoNothing;
                        }

                        return Enum.TryParse(enumType, parameterString, true, out var parsedValue)
                                ? parsedValue
                                : Binding.DoNothing;
                }
        }
}
