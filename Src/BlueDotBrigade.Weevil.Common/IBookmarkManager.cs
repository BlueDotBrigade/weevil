namespace BlueDotBrigade.Weevil
{
	using System.Collections.Immutable;

	public interface IBookmarkManager
	{
		ImmutableArray<Bookmark> Bookmarks { get; }

		void CreateFromSelection(int id, string bookmarkName, int lineNumber);

		void Clear();

		bool Clear(int lineNumber);

		public bool TryGetBookmarkName(int lineNumber, out string bookmarkName);

		public bool TryGetBookmarkById(int id, out Bookmark bookmark);

		public bool TryGetBookmark(int lineNumber, out Bookmark bookmark);
	}
}