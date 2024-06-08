namespace BlueDotBrigade.Weevil.Cli.IO
{
	using System.Diagnostics;
	using System;

	internal sealed class HtmlFormatter : IOutputFormatter
	{
		private int _numberedItemCounter = 1;

		public string AsText(string message) => message;
		public string AsHeading(string message) => $"<h1>{message}</h1>";
		public string AsBullet(string message) => $"<li>{message}</li>";
		public string AsNumbered(string message) => $"<li>{_numberedItemCounter++}. {message}</li>";
		public string AsError(string message) => $"<span style='color:red;'>ERROR: {message}</span>";
		public void ResetNumbering() => _numberedItemCounter = 1;
	}
}
