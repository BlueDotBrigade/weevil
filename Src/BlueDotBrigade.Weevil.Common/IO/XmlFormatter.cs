namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Linq;
	using System.Security;
	using System.Text;
	using System.Xml.Linq;

	public sealed class XmlFormatter : IOutputFormatter
	{
		private int _numberedItemCounter = 1;
		private bool _isTableOpen;

		public string AsText(string message) => CloseTableIfOpen() + $"<text>{SecurityElement.Escape(message)}</text>";
		public string AsHeading(string message) => CloseTableIfOpen() + $"<heading>{SecurityElement.Escape(message)}</heading>";
		public string AsSubHeading(string message) => CloseTableIfOpen() + $"<subheading>{SecurityElement.Escape(message)}</subheading>";
		public string AsBullet(string message) => CloseTableIfOpen() + $"<item>{SecurityElement.Escape(message)}</item>";
		public string AsNumbered(string message) => $"<item number=\"{_numberedItemCounter++}\">{SecurityElement.Escape(message)}</item>";
		public string AsError(string message) => CloseTableIfOpen() + $"<error>{SecurityElement.Escape(message)}</error>";

		public string AsTableHeader(string[] headers)
		{
			var sb = new StringBuilder();

			sb.Append(CloseTableIfOpen());

			sb.AppendLine("<table>");
			_isTableOpen = true;

			sb.AppendLine("  <columns>");
			foreach (var header in headers)
			{
				sb.AppendLine($"    <column>{SecurityElement.Escape(header)}</column>");
			}
			sb.Append("  </columns>");

			return sb.ToString();
		}

		public string AsTableRow(string[] columns)
		{
			var sb = new StringBuilder();

			sb.AppendLine("  <row>");
			foreach (var cell in columns)
			{
				sb.AppendLine($"    <cell>{SecurityElement.Escape(cell)}</cell>");
			}
			sb.Append("  </row>");

			return sb.ToString();
		}

		public string AsTable(string[] headers, string[][] rows)
		{
			var doc = new XDocument(
				new XDeclaration("1.0", "utf-8", null),
				new XElement("table",
					new XElement("columns",
						headers.Select(h => new XElement("column", h))
					),
					new XElement("rows",
						rows.Select(r => new XElement("row",
							r.Select(c => new XElement("cell", c))
						))
					)
				)
			);

			return doc.Declaration + Environment.NewLine + doc.Root;
		}

		public void ResetNumbering()
		{
			_numberedItemCounter = 1;
		}

		private string CloseTableIfOpen()
		{
			if (_isTableOpen)
			{
				_isTableOpen = false;
				return "</table>" + Environment.NewLine;
			}

			return string.Empty;
		}
	}
}
