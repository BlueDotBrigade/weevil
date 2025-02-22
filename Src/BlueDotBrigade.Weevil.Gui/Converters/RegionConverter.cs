namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Data;
	using System.Windows.Data;
	using System.Windows;
	using BlueDotBrigade.Weevil.Gui.Filter;

	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	using System;
	using System.Globalization;
	using System.Windows.Data;
	using global::BlueDotBrigade.Weevil.Data;
	using global::BlueDotBrigade.Weevil.Gui.Filter;

	public class RegionStringConverter : IMultiValueConverter
	{
		/// <summary>
		/// Expects:
		///   values[0]: The current IRecord (the item in the ListView)
		///   values[1]: The parent FilterViewModel (the DataContext of the UserControl/Window)
		///   values[2]: True indicates that a tooltip is being created.
		/// </summary>
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values?.Length < 2)
				return string.Empty;

			var isToolTip = false;
			var record = values[0] as IRecord;
			var viewModel = values[1] as FilterViewModel;

			if (record == null || viewModel == null)
				return string.Empty;

			if (values.Length == 3)
			{
				bool.TryParse(values[2].ToString(), out isToolTip);
			}

			if (viewModel.RegionStartsWith(record, out var regionName1))
			{
				return isToolTip
					? $"🡇 Start of region: {regionName1}" 
					: $"🡇 {regionName1} ";
			}
			else if (viewModel.RegionEndsWith(record, out var regionName2))
			{
				return isToolTip
					? $"🡅 End of region: {regionName2}"
					: $"🡅 {regionName2} ";
			}
			else
			{
				return string.Empty;
			}
			
			return string.Empty;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}