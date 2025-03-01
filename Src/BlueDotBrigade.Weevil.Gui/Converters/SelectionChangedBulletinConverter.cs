namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Filter;

	[ValueConversion(typeof(SelectionChangedBulletin), typeof(string))]
	public class SelectionChangedBulletinConverter : IValueConverter
	{
		public object Convert(object bulletin, Type targetType, object isMultiLine, CultureInfo culture)
		{
			var result = DependencyProperty.UnsetValue; // signal that conversion failed

			if (bulletin is SelectionChangedBulletin concreteBulletin)
			{
				if (!string.IsNullOrWhiteSpace(concreteBulletin.SectionName) &&
					!string.IsNullOrWhiteSpace(concreteBulletin.RegionName))
				{
					result = $"{concreteBulletin.RegionName} / {concreteBulletin.SectionName}";
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(concreteBulletin.SectionName))
					{
						result = $"{concreteBulletin.SectionName}";
					}

					if (!string.IsNullOrWhiteSpace(concreteBulletin.RegionName))
					{
						result = $"{concreteBulletin.RegionName}";
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
