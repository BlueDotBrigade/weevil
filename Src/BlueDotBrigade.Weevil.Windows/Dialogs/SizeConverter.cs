namespace BlueDotBrigade.Weevil.Windows.Dialogs
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	/// <summary>
	/// A file's size as Explorer shows it, in KB from 1 KB up, and nothing for a folder.
	/// </summary>
	public sealed class SizeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not long bytes)
			{
				return string.Empty;
			}

			return bytes < 1024 ? $"{bytes:N0} bytes" : $"{System.Math.Ceiling(bytes / 1024.0):N0} KB";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}
