namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	/// <summary>
	/// Values that satisfy Microsoft's <see cref="string.IsNullOrWhiteSpace"/> criteria are converted to a default value.
	/// </summary>
	[ValueConversion(typeof(string), typeof(string))]
	public class FilterConverter : IValueConverter
	{
		/// <summary>
		/// Converts the value into a filter string, or a <paramref name="fallbackValue"/>, to be displayed in a user control.
		/// </summary>
		/// <remarks>
		/// Data flowing from the source (e.g. DataContext/ViewModel) to the target (e.g. a user control).
		/// </remarks>
		public object Convert(object viewModelValue, Type targetType, object fallbackValue, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (fallbackValue != null)
			{
				if (viewModelValue == null)
				{
					result = fallbackValue;
				}
				else
				{
					if (string.IsNullOrWhiteSpace(viewModelValue.ToString()))
					{
						result = fallbackValue;
					}
					else
					{
						result = viewModelValue; // this is a real filter
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Converts the value into a filter string, or <see cref="string.Empty"/> string when the <paramref name="fallbackValue"/> is detected.
		/// </summary>
		/// <remarks>
		/// Data is typically flowing from the target (e.g. a user control) to the source (e.g. DataContext/ViewModel).
		/// </remarks>
		public object ConvertBack(object userControlValue, Type targetType, object fallbackValue, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed


			if (userControlValue == null)
			{
				result = string.Empty;
			}
			else
			{
				var targetValueString = userControlValue.ToString();

				if (string.IsNullOrWhiteSpace(targetValueString))
				{
					result = string.Empty;
				}
				else
				{
					// There is a filter value
					// ... but is it real?
					if (fallbackValue != null)
					{
						var fallbackValueString = fallbackValue.ToString();

						if (targetValueString == fallbackValueString)
						{
							result = string.Empty;
						}
						else
						{
							result = targetValueString;
						}
					}
				}
			}

			return result;
		}
	}
}
