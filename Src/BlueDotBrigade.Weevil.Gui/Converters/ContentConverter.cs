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
		private const int MaximumLength = 8 * 1024;
		private const int TruncatedLength = 1 * 256;

		private static readonly ShortenedRecordFormatter ShortenedRecordFormatter =
			new ShortenedRecordFormatter(MaximumLength, TruncatedLength);

		private static readonly SimpleCallStackFormatter CallStackFormatter = new SimpleCallStackFormatter();

		/// <summary>
		/// Ensures that the record content is not exceptionally long.
		/// </summary>
		/// <remarks>
		/// Converts data flowing from the source (e.g. DataContext/ViewModel) to the target (i.e. the user control).
		/// </remarks>
		public object Convert(object record, Type targetType, object parameter, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (record is IRecord concreteRecord)
			{
				var content = CallStackFormatter.Format(concreteRecord);
				result = ShortenedRecordFormatter.Format(content);
			}

			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
