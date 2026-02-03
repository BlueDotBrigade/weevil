namespace BlueDotBrigade.Weevil.IO
{
	using System;

	public sealed class HtmlFormatter : IOutputFormatter
	{
		private int _numberedItemCounter = 1;

		public string AsText(string message) => message;
		public string AsHeading(string message) => $"<h1>{message}</h1>";
		public string AsBullet(string message) => $"<li>{message}</li>";
		public string AsNumbered(string message) => $"<li>{_numberedItemCounter++}. {message}</li>";
		public string AsError(string message) => $"<span style='color:red;'>ERROR: {message}</span>";
		
		public string AsTable(string[] headers, string[][] rows)
		{
			var lines = new System.Collections.Generic.List<string>();
			
			lines.Add("<table>");
			
			// Header row
			lines.Add("  <thead>");
			lines.Add("    <tr>");
			foreach (var header in headers)
			{
				lines.Add($"      <th>{header}</th>");
			}
			lines.Add("    </tr>");
			lines.Add("  </thead>");
			
			// Data rows
			lines.Add("  <tbody>");
			foreach (var row in rows)
			{
				lines.Add("    <tr>");
				foreach (var cell in row)
				{
					lines.Add($"      <td>{cell}</td>");
				}
				lines.Add("    </tr>");
			}
			lines.Add("  </tbody>");
			
			lines.Add("</table>");
			
			return string.Join(Environment.NewLine, lines);
		}
		
		public void ResetNumbering() => _numberedItemCounter = 1;
	}
}
