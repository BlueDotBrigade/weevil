namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;

	[DebuggerDisplay("Count={_bookmarks.Count}")]
	internal class BookmarkManager : IBookmarkManager
	{
		private readonly List<Bookmark> _bookmarks;
		private readonly object _bookmarkPadlock;

		internal BookmarkManager() : this(ImmutableArray<Bookmark>.Empty)
		{
			// nothing to do
		}

		internal BookmarkManager(ImmutableArray<Bookmark> bookmarks)
		{
			_bookmarks = new List<Bookmark>(bookmarks);
			_bookmarkPadlock = new object();
		}

		public ImmutableArray<Bookmark> Bookmarks
		{
			get
			{
				lock (_bookmarkPadlock)
				{
					return _bookmarks.ToImmutableArray();
				}
			}
		}

		public void CreateFromSelection(string bookmarkName, int lineNumber)
		{
			lock (_bookmarkPadlock)
			{
				var bookmark = new Bookmark(bookmarkName, lineNumber);

				if (_bookmarks.Any(r => r.Record.LineNumber == bookmark.Record.LineNumber))
				{
					throw new InvalidOperationException("Unable to create bookmark because this bookmark has already been defined.");
				}

				_bookmarks.Add(bookmark);
			}
		}

		public bool TryGetBookmarkName(int lineNumber, out string bookmarkName)
		{			
			lock (_bookmarkPadlock)
			{
				Bookmark bookmark = _bookmarks.FirstOrDefault(r => r.Contains(lineNumber));

				if (bookmark == null)
				{
					bookmarkName = string.Empty;
					return false;
				}
				else
				{
					bookmarkName = bookmark.Name;
					return true;
				}
			}
		}
		public bool Contains(int lineNumber)
		{
			lock (_bookmarkPadlock)
			{
				return _bookmarks.Any(r => r.Contains(lineNumber));
			}
		}

		public void Clear()
		{
			lock (_bookmarkPadlock)
			{
				_bookmarks.Clear();
			}
		}

		public bool Clear(int lineNumber)
		{
			lock (_bookmarkPadlock)
			{
				Bookmark bookmark = _bookmarks.FirstOrDefault(r => r.Contains(lineNumber));

				if (bookmark != null)
				{
					_bookmarks.Remove(bookmark);
					return true;
				}
				return false;
			}
		}
	}
}
