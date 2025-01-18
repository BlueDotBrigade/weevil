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

	public class RegionStringConverter : IMultiValueConverter
	{
		/// <summary>
		/// Expects:
		///   values[0]: The current IRecord (the item in the ListView)
		///   values[1]: The parent FilterViewModel (the DataContext of the UserControl/Window)
		/// Returns a string indicating the record's region status:
		///   - "Start of Region"
		///   - "In Region"
		///   - "End of Region"
		///   - string.Empty (when the record isn't in any region)
		/// </summary>
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values?.Length < 2)
				return string.Empty;

			var record = values[0] as IRecord;
			var viewModel = values[1] as FilterViewModel;

			if (record == null || viewModel == null)
				return string.Empty;

			// You can define your own logic:
			// e.g., if region is a single continuous block, you might define:
			//  - starts with a region if ...
			//  - ends with a region if ...
			//  - or "in region" otherwise
			if (viewModel.RegionStartsWith(record))
			{
				return "Start of Region";
			}
			else if (viewModel.RegionEndsWith(record))
			{
				return "End of Region";
			}
			else if (viewModel.RegionContains(record))
			{
				return "In Region";
			}

			return string.Empty;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}