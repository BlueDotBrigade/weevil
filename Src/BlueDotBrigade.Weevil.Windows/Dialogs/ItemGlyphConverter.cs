namespace BlueDotBrigade.Weevil.Windows.Dialogs
{
	using System;
	using System.Globalization;
	using System.Windows.Data;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;

	/// <summary>
	/// The icon of a listed item: a cloud for an online-only file.
	/// </summary>
	public sealed class ItemGlyphConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is BrowserItem item
			? item.Kind switch
			{
				ItemKind.Drive => Glyphs.Drive,
				ItemKind.Folder => Glyphs.Folder,
				ItemKind.Container => Glyphs.Zip,
				_ => item.IsOnlineOnly ? Glyphs.Cloud : Glyphs.Document,
			}
			: string.Empty;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}
