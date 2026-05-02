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
		private readonly object _gate;

		internal BookmarkManager() : this(ImmutableArray<Bookmark>.Empty)
		{
			// nothing to do
		}

		internal BookmarkManager(ImmutableArray<Bookmark> bookmarks)
		{
			_bookmarks = new List<Bookmark>(bookmarks);
			_gate = new object();
		}

		public ImmutableArray<Bookmark> Bookmarks
		{
			get
			{
				lock (_gate)
				{
					return _bookmarks.ToImmutableArray();
				}
			}
		}

		public void Create(int id, string bookmarkName, int lineNumber)
		{
			lock (_gate)
			{
				var effectiveName = string.IsNullOrEmpty(bookmarkName)
					? "Bookmark"
					: bookmarkName;

				var bookmark = new Bookmark(id, effectiveName, lineNumber);

				if (_bookmarks.Any(r => r.Record.LineNumber == bookmark.Record.LineNumber))
				{
					throw new InvalidOperationException("Unable to create bookmark because this bookmark has already been defined.");
				}

				// If creating a bookmark with a specific ID (e.g., Ctrl+Shift+[1-5]),
				// remove any existing bookmark with that same ID first
				if (id > 0)
				{
					var existingBookmarkWithSameId = _bookmarks.FirstOrDefault(r => r.Id == id);
					if (existingBookmarkWithSameId != null)
					{
						_bookmarks.Remove(existingBookmarkWithSameId);
					}
				}

				_bookmarks.Add(bookmark);
			}
		}

		public bool TryGetBookmarkName(int lineNumber, out string bookmarkName)
		{			
			lock (_gate)
			{
				Bookmark bookmark = _bookmarks.FirstOrDefault(r => r.Record.LineNumber == lineNumber);

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

		public bool TryGetBookmarkById(int id, out Bookmark bookmark)
		{
			lock (_gate)
			{
				bookmark = _bookmarks.FirstOrDefault(r => r.Id == id);
				return bookmark != null;
			}
		}

		public bool TryGetBookmark(int lineNumber, out Bookmark bookmark)
		{
			lock (_gate)
			{
				bookmark = _bookmarks.FirstOrDefault(r => r.Record.LineNumber == lineNumber);
				return bookmark != null;
			}
		}

		public bool Contains(int lineNumber)
		{
			lock (_gate)
			{
				return _bookmarks.Any(r => r.Record.LineNumber == lineNumber);
			}
		}

		public void Clear()
		{
			lock (_gate)
			{
				_bookmarks.Clear();
			}
		}

		public bool Clear(int lineNumber)
		{
			lock (_gate)
			{
				Bookmark bookmark = _bookmarks.FirstOrDefault(r => r.Record.LineNumber == lineNumber);

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
