namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Generic;

	/// <summary>
	/// A plugin specific data structure that identifies key sections of a log file.
	/// </summary>
	/// <remarks>
	/// The core engine has no concept of what is stored in the <see cref="ITableOfContents"/>,
	/// as this data structure is populated by the underlying plugin.
	///
	/// For example, a plugin may use the <see cref="ITableOfContents"/> to highlight principal
	/// application state changes.
	/// </remarks>
	public interface ITableOfContents
	{
		string GetSection(int lineNumber);

		IReadOnlyList<ISection> Sections { get; }

		ISection this[int index] { get; }
	}
}