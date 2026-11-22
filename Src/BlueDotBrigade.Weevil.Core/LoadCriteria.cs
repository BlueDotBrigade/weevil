namespace BlueDotBrigade.Weevil
{
	using System;

	/// <summary>
	/// What an engine was asked to load, which Reload asks for again in the log as it is now (#1219).
	/// </summary>
	/// <param name="StartAtLineNumber">The line reading starts at.</param>
	/// <param name="Range">The line numbers asked for, when no region or section was named.</param>
	/// <param name="RegionName">The region asked for, found by name: otherwise <see langword="null"/>.</param>
	/// <param name="SectionName">The level-1 section asked for, found by name: otherwise <see langword="null"/>.</param>
	/// <param name="MaxRecords">The most records kept.</param>
	internal sealed record LoadCriteria(int StartAtLineNumber, Range Range, string RegionName, string SectionName, int MaxRecords);
}
