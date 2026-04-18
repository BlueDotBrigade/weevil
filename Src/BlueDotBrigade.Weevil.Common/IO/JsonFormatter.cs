namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Text.Json;

	public sealed class JsonFormatter : IOutputFormatter
	{
		private int _numberedItemCounter = 1;

		public string AsText(string message) => JsonSerializer.Serialize(new { text = message });
		public string AsHeading(string message) => JsonSerializer.Serialize(new { heading = message });
		public string AsSubHeading(string message) => JsonSerializer.Serialize(new { subHeading = message });
		public string AsBullet(string message) => JsonSerializer.Serialize(new { bullet = message });

		public string AsNumbered(string message) => JsonSerializer.Serialize(new { number = _numberedItemCounter++, text = message });

		public string AsError(string message) => JsonSerializer.Serialize(new { error = message });

		public string AsTableHeader(string[] headers) => JsonSerializer.Serialize(new { headers });

		public string AsTableRow(string[] columns) => JsonSerializer.Serialize(new { row = columns });

		public string AsTable(string[] headers, string[][] rows)
		{
			return JsonSerializer.Serialize(new { headers, rows });
		}

		public void ResetNumbering() => _numberedItemCounter = 1;
	}
}
