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
		
		public string AsTable(string[] headers, string[][] rows)
		{
			var lines = new System.Collections.Generic.List<string>();
			
			// Header row
			lines.Add("| " + string.Join(" | ", headers) + " |");
			
			// Separator row
			var separators = new string[headers.Length];
			for (int i = 0; i < headers.Length; i++)
			{
				separators[i] = "---";
			}
			lines.Add("| " + string.Join(" | ", separators) + " |");
			
			// Data rows
			foreach (var row in rows)
			{
				lines.Add("| " + string.Join(" | ", row) + " |");
			}
			
			return string.Join(Environment.NewLine, lines);
		}
		
		public void ResetNumbering() => _numberedItemCounter = 1;
	}
}
