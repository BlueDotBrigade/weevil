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
				if (!string.IsNullOrWhiteSpace(concreteBulletin.CurrentSection) &&
					!string.IsNullOrWhiteSpace(concreteBulletin.CurrentRegion))
				{
					result = $"ROI-{concreteBulletin.CurrentRegion}: {concreteBulletin.CurrentSection}";
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(concreteBulletin.CurrentSection))
					{
						result = $"{concreteBulletin.CurrentSection}";
					}

					if (!string.IsNullOrWhiteSpace(concreteBulletin.CurrentRegion))
					{
						result = $"ROI-{concreteBulletin.CurrentRegion}";
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
