﻿namespace BlueDotBrigade.Weevil.Gui.Converters
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

	public class BookmarkStringConverter : IMultiValueConverter
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

			var record = values[0] as IRecord;
			var viewModel = values[1] as FilterViewModel;

			if (record == null || viewModel == null)
				return string.Empty;

			return "Bookmark";
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}