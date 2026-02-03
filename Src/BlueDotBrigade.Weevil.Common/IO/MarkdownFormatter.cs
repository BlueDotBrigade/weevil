namespace BlueDotBrigade.Weevil.IO
{
	using System;

	public sealed class MarkdownFormatter : IOutputFormatter
	{
		private int _numberedItemCounter = 1;

		public string AsText(string message) => message;
		public string AsHeading(string message) => $"# {message}";
		public string AsBullet(string message) => $"* {message}";
		public string AsNumbered(string message) => $"{_numberedItemCounter++}. {message}";
		public string AsError(string message) => $"**ERROR**: {message}";
		public void ResetNumbering() => _numberedItemCounter = 1;
	}
}
