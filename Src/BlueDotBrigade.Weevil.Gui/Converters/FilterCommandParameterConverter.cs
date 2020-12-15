namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	/// <summary>
	/// Intended to be used by <see cref="IMultiValueConverter"/> bindings, where the behavior of the
	/// <see cref="Convert"/> and <see cref="ConvertBack"/> methods are reversed.
	/// </summary>
	[ValueConversion(typeof(string), typeof(string))]
	public class FilterCommandParameterConverter : IValueConverter
	{
		private readonly FilterConverter _filterConverter = new FilterConverter();

		/// <summary>
		/// Converts the value into a filter string, or <see cref="string.Empty"/> string when the <paramref name="fallbackValue"/> is detected.
		/// </summary>
		/// <remarks>
		/// Data is typically flowing from the target (e.g. a user control) to the source (e.g. DataContext/ViewModel).
		/// </remarks>
		public object Convert(object userControlValue, Type targetType, object fallbackValue, CultureInfo culture)
		{
			return _filterConverter.ConvertBack(userControlValue, targetType, fallbackValue, culture);
		}

		public object ConvertBack(object viewModelValue, Type targetType, object fallbackValue, CultureInfo culture)
		{
			throw new NotImplementedException($"The {nameof(ConvertBack)} method is not supported by the {nameof(FilterCommandParameterConverter)} converter.");
		}
	}
}
