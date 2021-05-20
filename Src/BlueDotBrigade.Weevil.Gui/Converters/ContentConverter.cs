namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using BlueDotBrigade.Weevil.Data;

	[ValueConversion(typeof(string), typeof(string))]
	public class ContentConverter : IValueConverter
	{
		private static readonly SimpleCallStackFormatter CallstackFormatter = new SimpleCallStackFormatter();

		/// <summary>
		/// Ensures that the record content is not exceptionally long.
		/// </summary>
		/// <remarks>
		/// Converts data flowing from the source (e.g. DataContext/ViewModel) to the target (i.e. the user control).
		/// </remarks>
		public object Convert(object record, Type targetType, object isMultiLine, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (record is IRecord concreteRecord)
			{
				result = CallstackFormatter.Format(concreteRecord);
			}

			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
