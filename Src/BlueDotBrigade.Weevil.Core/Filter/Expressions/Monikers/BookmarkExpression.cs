namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class BookmarkExpression : IExpression
	{
		public static readonly Moniker Moniker = new Moniker("@Bookmark");

		private readonly IBookmarkManager _bookmarkManager;

		public BookmarkExpression(string serializedExpression, IBookmarkManager bookmarkManager)
		{
			_bookmarkManager = bookmarkManager;
		}

		public bool IsMatch(IRecord record)
		{
			return
				_bookmarkManager.TryGetBookmarkName(record.LineNumber, out _);
		}
	}
}
