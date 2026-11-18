namespace BlueDotBrigade.Weevil.Windows.Dialogs
{
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
}
