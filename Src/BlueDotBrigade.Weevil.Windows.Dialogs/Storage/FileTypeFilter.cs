namespace BlueDotBrigade.Weevil.Windows.Dialogs.Storage
{
	using System;
	using System.Collections.Generic;
	using System.IO.Enumeration;
	using System.Linq;

	/// <summary>
	/// A choice in the file-type list, such as "Log files": the names it shows, and those it hides even so.
	/// </summary>
	/// <remarks>
	/// Patterns use <c>*</c> and <c>?</c>, ignore case, and match the whole name. Folders and zips are always shown.
	/// They are matched as the file system matches them, without a regular expression, so no pattern can make a match slow.
	/// </remarks>
	public sealed class FileTypeFilter
	{
		public static readonly FileTypeFilter AllFiles = new FileTypeFilter("All files", new[] { "*" });

		public FileTypeFilter(string name, IReadOnlyList<string> include, IReadOnlyList<string> exclude = null)
		{
			ArgumentException.ThrowIfNullOrEmpty(name);
			ArgumentNullException.ThrowIfNull(include);

			this.Name = name;
			this.Include = include;
			this.Exclude = exclude ?? Array.Empty<string>();
		}

		public string Name { get; }

		public IReadOnlyList<string> Include { get; }

		public IReadOnlyList<string> Exclude { get; }

		public bool Matches(string fileName)
		{
			return MatchesAny(this.Include, fileName) && !MatchesAny(this.Exclude, fileName);
		}

		public override string ToString() => this.Name;

		private static bool MatchesAny(IEnumerable<string> patterns, string fileName)
		{
			return patterns.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, fileName, ignoreCase: true));
		}
	}
}
