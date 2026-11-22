namespace BlueDotBrigade.Weevil.IO
{
	using System;

	/// <summary>
	/// The part of a log that is open: the whole log, one section, one region, or a range of lines (#1219).
	/// </summary>
	/// <remarks>
	/// <see cref="IEngine.Reload"/> opens the same part again, in the log as it is now: a section or region found again by its name,
	/// lines from a start line to the end of the log, so appended lines appear, or the same range of lines.
	/// </remarks>
	public sealed class LogPart
	{
		/// <summary>
		/// The whole log.
		/// </summary>
		public static readonly LogPart Whole = new LogPart(null, null, Range.All);

		private LogPart(string sectionName, string regionName, Range lines)
		{
			this.SectionName = sectionName;
			this.RegionName = regionName;
			this.Lines = lines;
		}

		/// <summary>
		/// The name of the level-1 section open: otherwise <see langword="null"/>.
		/// </summary>
		public string SectionName { get; }

		/// <summary>
		/// The name of the region open: otherwise <see langword="null"/>.
		/// </summary>
		public string RegionName { get; }

		/// <summary>
		/// The line numbers asked for: <see cref="Range.All"/> for the whole log, and for a section or region, its lines when it was opened.
		/// </summary>
		/// <remarks>
		/// An end of <see cref="int.MaxValue"/> is the end of the log.
		/// </remarks>
		public Range Lines { get; }

		/// <summary>
		/// Whether the whole log is open.
		/// </summary>
		public bool IsWhole => this.SectionName is null && this.RegionName is null && this.Lines.Equals(Range.All);

		/// <summary>
		/// Whether the lines run to the end of the log, so that lines appended to it appear after Reload.
		/// </summary>
		public bool IsToEndOfLog => this.Lines.End.Value == int.MaxValue;

		/// <param name="name">The section's name.</param>
		/// <param name="lines">Its lines.</param>
		/// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
		public static LogPart ForSection(string name, Range lines)
		{
			return string.IsNullOrWhiteSpace(name)
				? throw new ArgumentException("A section's name is expected.", nameof(name))
				: new LogPart(name, null, lines);
		}

		/// <param name="name">The region's name.</param>
		/// <param name="lines">Its lines.</param>
		/// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
		public static LogPart ForRegion(string name, Range lines)
		{
			return string.IsNullOrWhiteSpace(name)
				? throw new ArgumentException("A region's name is expected.", nameof(name))
				: new LogPart(null, name, lines);
		}

		/// <param name="lines">The line numbers. <see cref="Range.All"/> is the whole log.</param>
		public static LogPart ForLines(Range lines)
		{
			return lines.Equals(Range.All) ? Whole : new LogPart(null, null, lines);
		}
	}
}
