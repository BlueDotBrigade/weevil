namespace BlueDotBrigade.Weevil
{
	using System.Collections.Immutable;

	public interface IBookmarkManager
	{
		ImmutableArray<Bookmark> Bookmarks { get; }

		void CreateFromSelection(string bookmarkName, int lineNumber);

		void Clear();

		bool Clear(int lineNumber);

		public bool TryGetBookmarkName(int lineNumber, out string bookmarkName);
	}
}