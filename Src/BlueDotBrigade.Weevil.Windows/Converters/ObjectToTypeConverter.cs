namespace BlueDotBrigade.Weevil.Windows.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	[ValueConversion(typeof(object), typeof(Type))]
	public class ObjectToTypeConverter : IValueConverter
	{
		/// <summary>
		/// Returns the the <see cref="Type"/> of the given <paramref name="value"/>.
		/// </summary>
		/// <seealso href="https://stackoverflow.com/a/4965683/949681">StackOverflow: WPF Trigger based on Object Type</seealso>
		public object Convert(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			return value?.GetType() ?? Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}