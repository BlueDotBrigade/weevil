namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using BlueDotBrigade.Weevil.Data;

	[ValueConversion(typeof(string), typeof(string))]
	public class ContentConverter : IValueConverter
	{
		private const int MaximumLength = 8 * 1024;
		private const int TrimmedLength = 1 * 256;

		internal static readonly string EndOfLine = "\u22EF";

		static ContentConverter()
		{
			Debug.Assert(MaximumLength > TrimmedLength);
		}

		/// <summary>
		/// Ensures that the record content is not exceptionally long.
		/// </summary>
		/// <remarks>
		/// Converts data flowing from the source (e.g. DataContext/ViewModel) to the target (i.e. the user control).
		/// </remarks>
		public object Convert(object record, Type targetType, object isMultiLine, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (record != null)
			{
				if (record is IRecord concreteRecord)
				{
					result = TruncateIf(concreteRecord.Content, !concreteRecord.Metadata.IsMultiLine, MaximumLength, TrimmedLength);
				}
			}

			return result;
		}

		internal static object TruncateIf(string content, bool isSingleLine, int maximumLength, int truncatedLength)
		{
			var result = content;

			if (isSingleLine)
			{
				if (content.Length >= maximumLength)
				{
					var maxLength = content.Length >= maximumLength
						? truncatedLength
						: content.Length;
					result = content.Substring(0, maxLength) + EndOfLine;
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
