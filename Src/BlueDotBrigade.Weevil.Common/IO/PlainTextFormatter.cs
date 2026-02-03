namespace BlueDotBrigade.Weevil.IO
{
	using System;

	public sealed class PlainTextFormatter : IOutputFormatter
	{
		private int _numberedItemCounter = 1;

		public string AsText(string message) => message;
		public string AsHeading(string message) => message.ToUpper();
		public string AsBullet(string message) => $"- {message}";
		public string AsNumbered(string message) => $"{_numberedItemCounter++}. {message}";
		public string AsError(string message) => $"ERROR: {message}";
		
		public string AsTable(string[] headers, string[][] rows)
		{
			var lines = new System.Collections.Generic.List<string>();
			
			// Header row (tab-delimited)
			lines.Add(string.Join("\t", headers));
			
			// Data rows (tab-delimited)
			foreach (var row in rows)
			{
				lines.Add(string.Join("\t", row));
			}
			
			return string.Join(Environment.NewLine, lines);
		}
		
		public void ResetNumbering() => _numberedItemCounter = 1;
	}
}
