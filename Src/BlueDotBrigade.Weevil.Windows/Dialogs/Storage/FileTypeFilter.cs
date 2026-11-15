namespace BlueDotBrigade.Weevil.Windows.Dialogs.Storage
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;

	/// <summary>
	/// A choice in the file-type list, such as "Log files": the names it shows, and those it hides even so.
	/// </summary>
	/// <remarks>
	/// Patterns use <c>*</c> and <c>?</c>, ignore case, and match the whole name. Folders and zips are always shown.
	/// </remarks>
	public sealed class FileTypeFilter
	{
		public static readonly FileTypeFilter AllFiles = new FileTypeFilter("All files", new[] { "*" });

		private readonly Regex _include;
		private readonly Regex _exclude;

		public FileTypeFilter(string name, IReadOnlyList<string> include, IReadOnlyList<string> exclude = null)
		{
			ArgumentException.ThrowIfNullOrEmpty(name);
			ArgumentNullException.ThrowIfNull(include);

			this.Name = name;
			this.Include = include;
			this.Exclude = exclude ?? Array.Empty<string>();
			_include = ToRegex(this.Include);
			_exclude = this.Exclude.Count == 0 ? null : ToRegex(this.Exclude);
		}

		public string Name { get; }

		public IReadOnlyList<string> Include { get; }

		public IReadOnlyList<string> Exclude { get; }

		public bool Matches(string fileName)
		{
			return _include.IsMatch(fileName) && (_exclude is null || !_exclude.IsMatch(fileName));
		}

		public override string ToString() => this.Name;

		private static Regex ToRegex(IEnumerable<string> patterns)
		{
			var alternatives = patterns.Select(pattern => Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", "."));
			return new Regex($"^(?:{string.Join("|", alternatives)})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		}
	}
}
