namespace OpenAsDialogPoC
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;

	/// <summary>
	/// A section of a log's table of contents: from its line to the line before the next section at the same or a higher level.
	/// </summary>
	/// <param name="LastLine">The section's last line, or <see langword="null"/> when it runs to the end of the log.</param>
	public sealed record SidecarSection(string Name, int Level, int FirstLine, int? LastLine)
	{
		public override string ToString()
		{
			var lines = this.LastLine is int last ? $"lines {this.FirstLine:N0}–{last:N0}" : $"line {this.FirstLine:N0} to the end";
			return $"{new string(' ', System.Math.Max(0, this.Level - 1) * 3)}{this.Name}  ({lines})";
		}
	}

	public sealed record SidecarRegion(string Name, int FirstLine, int LastLine)
	{
		public override string ToString() => $"{this.Name}  (lines {this.FirstLine:N0}–{this.LastLine:N0})";
	}

	/// <summary>
	/// Reads a Weevil sidecar's sections and regions with plain XML: a stand-in for Weevil's own loader, so the proof of concept needs no Weevil code.
	/// </summary>
	public static class Sidecar
	{
		public static (IReadOnlyList<SidecarSection> Sections, IReadOnlyList<SidecarRegion> Regions) Read(Stream stream)
		{
			XDocument document = XDocument.Load(stream);

			var starts = document.Descendants()
				.Where(element => element.Name.LocalName == "Section")
				.Select(element => (
					Name: Child(element, "Name") ?? "(no name)",
					Level: ToInt(Child(element, "Level")) ?? 1,
					Line: ToInt(element.Descendants().FirstOrDefault(child => child.Name.LocalName == "LineNumber")?.Value)))
				.Where(section => section.Line is not null)
				.OrderBy(section => section.Line)
				.ToList();

			var sections = new List<SidecarSection>();

			for (var index = 0; index < starts.Count; index++)
			{
				var start = starts[index];
				var next = starts.Skip(index + 1).FirstOrDefault(other => other.Level <= start.Level);
				sections.Add(new SidecarSection(start.Name, start.Level, start.Line.Value, next.Line is int nextLine ? nextLine - 1 : null));
			}

			var regions = document.Descendants()
				.Where(element => element.Name.LocalName == "Region")
				.Select(element => (
					Name: Child(element, "Name") ?? "(no name)",
					First: ToInt(LineOf(element, "Minimum")),
					Last: ToInt(LineOf(element, "Maximum"))))
				.Where(region => region.First is not null && region.Last is not null)
				.Select(region => new SidecarRegion(region.Name, region.First.Value, region.Last.Value))
				.OrderBy(region => region.FirstLine)
				.ToList();

			return (sections, regions);
		}

		private static string Child(XElement element, string name) => element.Elements().FirstOrDefault(child => child.Name.LocalName == name)?.Value;

		private static string LineOf(XElement region, string boundary)
		{
			return region.Elements().FirstOrDefault(child => child.Name.LocalName == boundary)?
				.Elements().FirstOrDefault(child => child.Name.LocalName == "LineNumber")?.Value;
		}

		private static int? ToInt(string text) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
	}
}
