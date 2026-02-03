namespace BlueDotBrigade.Weevil.IO
{
	using System;

	/// <summary>
	/// Formats output as tab-delimited text, suitable for spreadsheet applications.
	/// </summary>
	public sealed class TabDelimitedFormatter : IOutputFormatter
	{
		private int _numberedItemCounter = 1;

		public string AsText(string message) => message;
		public string AsHeading(string message) => message;
		public string AsBullet(string message) => message;
		public string AsNumbered(string message) => $"{_numberedItemCounter++}.\t{message}";
		public string AsError(string message) => $"ERROR:\t{message}";
		public void ResetNumbering() => _numberedItemCounter = 1;
	}
}
