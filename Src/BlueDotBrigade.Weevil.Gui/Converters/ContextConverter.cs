namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using BlueDotBrigade.Weevil;

	[ValueConversion(typeof(ContextDictionary), typeof(string))]
	public class ContextConverter : IValueConverter
	{
		/// <summary>
		/// Converts the <see cref="ContextDictionary"/> value to a label that can be displayed to the user.
		/// </summary>
		/// <remarks>
		/// Converts data flowing from the source (e.g. DataContext/ViewModel) to the target (i.e. the user control).
		/// </remarks>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (value != null)
			{
				if (value is ContextDictionary context)
				{
					if (context.Count > 0)
					{
						result = context.ToString();
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
