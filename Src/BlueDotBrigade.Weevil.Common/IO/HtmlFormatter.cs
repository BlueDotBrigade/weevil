namespace BlueDotBrigade.Weevil.IO
{
	using System;

	public sealed class HtmlFormatter : IOutputFormatter
	{
		private int _numberedItemCounter = 1;

		public string AsText(string message) => message;
		public string AsHeading(string message) => $"<h1>{message}</h1>";
		public string AsSubHeading(string message) => $"<h2>{message}</h2>";
		public string AsBullet(string message) => $"<li>{message}</li>";
		public string AsNumbered(string message) => $"<li>{_numberedItemCounter++}. {message}</li>";
		public string AsError(string message) => $"<span style='color:red;'>ERROR: {message}</span>";
		public string AsWarning(string message) => $"<span style='color:orange;'>WARNING: {message}</span>";
		public string AsTableHeader(string[] headers)
		{
			var lines = new System.Collections.Generic.List<string>
			{
				"  <thead>",
				"    <tr>"
			};

			foreach (var header in headers)
			{
				lines.Add($"      <th>{header}</th>");
			}

			lines.Add("    </tr>");
			lines.Add("  </thead>");
			return string.Join(Environment.NewLine, lines);
		}

		public string AsTableRow(string[] columns)
		{
			var lines = new System.Collections.Generic.List<string>
			{
				"    <tr>"
			};

			foreach (var cell in columns)
			{
				lines.Add($"      <td>{cell}</td>");
			}

			lines.Add("    </tr>");
			return string.Join(Environment.NewLine, lines);
		}
		
		public string AsTable(string[] headers, string[][] rows)
		{
			var lines = new System.Collections.Generic.List<string>();
			
			lines.Add("<table>");
			
			// Header row
			lines.Add(AsTableHeader(headers));
			
			// Data rows
			lines.Add("  <tbody>");
			foreach (var row in rows)
			{
				lines.Add(AsTableRow(row));
			}
			lines.Add("  </tbody>");
			
			lines.Add("</table>");
			
			return string.Join(Environment.NewLine, lines);
		}
		
		public void ResetNumbering() => _numberedItemCounter = 1;
	}
}
