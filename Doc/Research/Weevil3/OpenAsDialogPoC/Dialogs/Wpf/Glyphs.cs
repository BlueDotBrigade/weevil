namespace BlueDotBrigade.Windows.Dialogs
{
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using System.Windows.Data;
	using BlueDotBrigade.Windows.Dialogs.Storage;

	/// <summary>
	/// Icons from the fonts Windows ships, Segoe Fluent Icons on Windows 11 and Segoe MDL2 Assets on Windows 10, so no icon package is needed.
	/// </summary>
	public static class Glyphs
	{
		// A FontFamily, not a string: XAML does not convert what {x:Static} returns, so a string would crash the dialog.
		public static readonly System.Windows.Media.FontFamily FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

		public const string Back = "";
		public const string Forward = "";
		public const string Up = "";
		public const string Refresh = "";
		public const string Folder = "";
		public const string Document = "";
		public const string Zip = "";
		public const string Drive = "";
		public const string Cloud = "";
		public const string Computer = "";
		public const string Desktop = "";
		public const string Download = "";
		public const string Recent = "";
	}

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

	/// <summary>
	/// The places in the navigation pane.
	/// </summary>
	public sealed class Place
	{
		public Place(string name, string glyph, Location location)
		{
			this.Name = name;
			this.Glyph = glyph;
			this.Location = location;
		}

		public string Name { get; }

		public string Glyph { get; }

		public Location Location { get; }

		public override string ToString() => this.Name;
	}

	internal static class KnownFolders
	{
		private static readonly Guid DownloadsId = new Guid("374DE290-123F-4565-9164-39C4925E467B");

		/// <summary>
		/// The Downloads folder, which <see cref="Environment.SpecialFolder"/> lacks, or <see langword="null"/>.
		/// </summary>
		public static string GetDownloads()
		{
			if (!OperatingSystem.IsWindows())
			{
				return null;
			}

			if (SHGetKnownFolderPath(DownloadsId, 0, IntPtr.Zero, out IntPtr path) != 0)
			{
				return null;
			}

			try
			{
				return Marshal.PtrToStringUni(path);
			}
			finally
			{
				Marshal.FreeCoTaskMem(path);
			}
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid folderId, uint flags, IntPtr token, out IntPtr path);
	}
}
