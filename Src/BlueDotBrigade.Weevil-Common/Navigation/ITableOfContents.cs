namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Generic;

	public interface ITableOfContents
	{
		string GetSection(int lineNumber);

		IReadOnlyList<ISection> Sections { get; }

		ISection this[int index] { get; }
	}
}