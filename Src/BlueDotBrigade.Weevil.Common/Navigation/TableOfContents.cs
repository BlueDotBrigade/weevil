namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	[DebuggerDisplay("Count={_sections.Count}")]
	public class TableOfContents : ITableOfContents
	{
		private readonly IReadOnlyList<Section> _sections;

		public TableOfContents()
		{
			_sections = new List<Section>();
		}

		public TableOfContents(List<Section> sections)
		{
			_sections = sections
				.OrderBy(x => x.LineNumber)
				.ThenBy(x => x.Level)
				.ToList();
		}

		public IReadOnlyList<Section> Sections => _sections;

		IReadOnlyList<ISection> ITableOfContents.Sections => _sections;

		public Section this[int index] => _sections[index];

		ISection ITableOfContents.this[int index] => _sections[index];

		public bool TryGetSectionName(int lineNumber, out string sectionName)
		{
			sectionName = string.Empty;

			if (_sections.Count > 1)
			{
				// Is the line number within the table of contents?
				if (lineNumber >= _sections[0].LineNumber)
				{
					foreach (Section section in _sections)
					{
						if (section.Level == 2)
						{
							if (lineNumber >= section.LineNumber)
							{
								sectionName = section.Name;
							}
							else
							{
								break;
							}
						}
					}
				}
			}

			return !string.IsNullOrWhiteSpace(sectionName);
		}
	}
}
