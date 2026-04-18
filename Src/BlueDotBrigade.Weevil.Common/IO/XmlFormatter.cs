namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Xml;
	using System.Xml.Linq;

	public sealed class XmlFormatter : IOutputFormatter
	{
		private static readonly Regex HeaderTokenRegex = new("[A-Za-z0-9]+", RegexOptions.Compiled);
		private int _numberedItemCounter = 1;
		private bool _isTableOpen;
		private string[] _activeColumnNames = Array.Empty<string>();

		public string AsText(string message) => CloseTableIfOpen() + CreateElement("Text", message);
		public string AsHeading(string message) => CloseTableIfOpen() + CreateElement("Heading", message);
		public string AsSubHeading(string message) => CloseTableIfOpen() + CreateElement("SubHeading", message);
		public string AsBullet(string message) => CloseTableIfOpen() + CreateElement("Item", message);
		public string AsNumbered(string message)
		{
			return CloseTableIfOpen()
				+ new XElement("Item", new XAttribute("Number", _numberedItemCounter++), message ?? string.Empty)
					.ToString(SaveOptions.DisableFormatting);
		}
		public string AsError(string message) => CloseTableIfOpen() + CreateElement("Error", message);
		public string AsWarning(string message) => CloseTableIfOpen() + CreateElement("Warning", message);

		public string AsTableHeader(string[] headers)
		{
			var preface = CloseTableIfOpen();
			_activeColumnNames = BuildColumnNames(headers).ToArray();
			_isTableOpen = true;

			return $"{preface}<Table>{Environment.NewLine}{CreateColumnsElement(_activeColumnNames)}";
		}

		public string AsTableRow(string[] columns)
		{
			var columnNames = ResolveColumnNames(columns.Length);
			var rowElement = new XElement("Row");

			for (var i = 0; i < columns.Length; i++)
			{
				rowElement.Add(new XElement(columnNames[i], columns[i] ?? string.Empty));
			}

			return rowElement.ToString();
		}

		public string AsTable(string[] headers, string[][] rows)
		{
			var columnNames = BuildColumnNames(headers).ToArray();
			var doc = new XDocument(
				new XDeclaration("1.0", "utf-8", null),
				new XElement("Table",
					CreateColumnsElement(columnNames),
					new XElement("Rows",
						rows.Select(r => CreateRowElement(columnNames, r))
					)
				)
			);

			return Serialize(doc);
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
				_activeColumnNames = Array.Empty<string>();
				return "</Table>" + Environment.NewLine;
			}

			return string.Empty;
		}

		private static string CreateElement(string name, string value) =>
			new XElement(name, value ?? string.Empty).ToString(SaveOptions.DisableFormatting);

		private static XElement CreateColumnsElement(IEnumerable<string> columnNames) =>
			new XElement("Columns", columnNames.Select(name => new XElement(name)));

		private static XElement CreateRowElement(IReadOnlyList<string> columnNames, string[] row)
		{
			var rowElement = new XElement("Row");
			for (var i = 0; i < row.Length; i++)
			{
				var columnName = i < columnNames.Count ? columnNames[i] : $"Column{i + 1}";
				rowElement.Add(new XElement(columnName, row[i] ?? string.Empty));
			}

			return rowElement;
		}

		private string[] ResolveColumnNames(int requiredCount)
		{
			if (_activeColumnNames.Length == requiredCount && requiredCount > 0)
			{
				return _activeColumnNames;
			}

			return Enumerable.Range(1, requiredCount).Select(i => $"Column{i}").ToArray();
		}

		private static IEnumerable<string> BuildColumnNames(string[] headers)
		{
			var uniqueNames = new HashSet<string>(StringComparer.Ordinal);

			for (var i = 0; i < headers.Length; i++)
			{
				var baseName = ToTitleCaseIdentifier(headers[i], i + 1);
				var name = baseName;
				var suffix = 2;

				while (!uniqueNames.Add(name))
				{
					name = $"{baseName}{suffix++}";
				}

				yield return name;
			}
		}

		private static string ToTitleCaseIdentifier(string value, int ordinal)
		{
			var tokens = HeaderTokenRegex.Matches(value ?? string.Empty)
				.Select(m => m.Value)
				.Where(t => t.Length > 0)
				.Select(ToTitleCaseToken)
				.ToArray();

			var candidate = tokens.Length == 0
				? $"Column{ordinal}"
				: string.Concat(tokens);

			if (char.IsDigit(candidate[0]))
			{
				candidate = $"Column{candidate}";
			}

			return candidate;
		}

		private static string ToTitleCaseToken(string token) =>
			token.Length == 1
				? token.ToUpperInvariant()
				: char.ToUpperInvariant(token[0]) + token.Substring(1).ToLowerInvariant();

		private static string Serialize(XDocument document)
		{
			using var stringWriter = new Utf8StringWriter();
			using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
			{
				Indent = true,
				OmitXmlDeclaration = false
			});

			document.WriteTo(xmlWriter);
			xmlWriter.Flush();
			return stringWriter.ToString();
		}

		private sealed class Utf8StringWriter : StringWriter
		{
			public override Encoding Encoding => Encoding.UTF8;
		}
	}
}
