namespace BlueDotBrigade.Weevil
{
	/// <summary>
	/// The 10 million record droid log the performance tests open (#1103), in each encoding whose bytes Weevil keeps.
	/// </summary>
	/// <remarks>
	/// The logs hold the same text. DatenLokator unzips them from `.Daten/.Global`, where `DroidLogGenerator.cs` writes them.
	/// </remarks>
	internal static class DroidLog
	{
		public const string Utf8 = "10MillionRecords_UTF8.log";

		public const string Utf16LittleEndian = "10MillionRecords_UTF16LE.log";

		public const int RecordCount = 10_000_000;

		public static string GetPath(string logName) => new Daten().AsFilePath(logName);
	}
}
