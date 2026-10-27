namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Linq;
	using System.Collections.Immutable;
	using System.Diagnostics;

	/// <remarks>
	/// Writes are serialized by the session's lock (#1107). Each write publishes a new array, so reads never wait.
	/// </remarks>
	[DebuggerDisplay("Count={_bookmarks.Length}")]
	internal class BookmarkManager : IBookmarkManager
	{
		private readonly object _writeLock;
		private ImmutableArray<Bookmark> _bookmarks = ImmutableArray<Bookmark>.Empty;

		internal BookmarkManager() : this(new object())
		{
			// nothing to do
		}

		/// <param name="writeLock">The lock that serializes writes, shared by every store of the session.</param>
		internal BookmarkManager(object writeLock)
		{
			_writeLock = writeLock ?? throw new ArgumentNullException(nameof(writeLock));
		}

		public ImmutableArray<Bookmark> Bookmarks => _bookmarks;

		/// <summary>
		/// Replaces every bookmark, such as with those read from the sidecar.
		/// </summary>
		internal void Load(ImmutableArray<Bookmark> bookmarks)
		{
			lock (_writeLock)
			{
				Publish(bookmarks.IsDefault ? ImmutableArray<Bookmark>.Empty : bookmarks);
			}
		}

		public void Create(int id, string bookmarkName, int lineNumber)
		{
			lock (_writeLock)
			{
				ImmutableArray<Bookmark> bookmarks = _bookmarks;

				var effectiveName = string.IsNullOrEmpty(bookmarkName)
					? "Bookmark"
					: bookmarkName;

				var bookmark = new Bookmark(id, effectiveName, lineNumber);

				if (bookmarks.Any(r => r.Record.LineNumber == bookmark.Record.LineNumber))
				{
					throw new InvalidOperationException("Unable to create bookmark because this bookmark has already been defined.");
				}

				// If creating a bookmark with a specific ID (e.g., Ctrl+Shift+[1-5]),
				// remove any existing bookmark with that same ID first
				if (id > 0)
				{
					Bookmark existingBookmarkWithSameId = bookmarks.FirstOrDefault(r => r.Id == id);
					if (existingBookmarkWithSameId != null)
					{
						bookmarks = bookmarks.Remove(existingBookmarkWithSameId);
					}
				}

				Publish(bookmarks.Add(bookmark));
			}
		}

		public bool Rename(int lineNumber, string newName)
		{
			lock (_writeLock)
			{
				ImmutableArray<Bookmark> bookmarks = _bookmarks;
				Bookmark existing = bookmarks.FirstOrDefault(r => r.Record.LineNumber == lineNumber);

				if (existing == null)
				{
					return false;
				}

				var effectiveName = string.IsNullOrEmpty(newName)
					? "Bookmark"
					: newName;

				var renamed = new Bookmark(existing.Id, effectiveName, existing.Record);
				Publish(bookmarks.Remove(existing).Add(renamed));
				return true;
			}
		}

		public bool TryGetBookmarkName(int lineNumber, out string bookmarkName)
		{
			// A loop rather than LINQ: filtering calls this for every record, and a lambda would allocate each time.
			foreach (Bookmark bookmark in _bookmarks)
			{
				if (bookmark.Record.LineNumber == lineNumber)
				{
					bookmarkName = bookmark.Name;
					return true;
				}
			}

			bookmarkName = string.Empty;
			return false;
		}

		public bool TryGetBookmarkById(int id, out Bookmark bookmark)
		{
			bookmark = _bookmarks.FirstOrDefault(r => r.Id == id);
			return bookmark != null;
		}

		public bool TryGetBookmark(int lineNumber, out Bookmark bookmark)
		{
			bookmark = _bookmarks.FirstOrDefault(r => r.Record.LineNumber == lineNumber);
			return bookmark != null;
		}

		public bool Contains(int lineNumber)
		{
			return _bookmarks.Any(r => r.Record.LineNumber == lineNumber);
		}

		public void Clear()
		{
			lock (_writeLock)
			{
				Publish(ImmutableArray<Bookmark>.Empty);
			}
		}

		public bool Clear(int lineNumber)
		{
			lock (_writeLock)
			{
				ImmutableArray<Bookmark> bookmarks = _bookmarks;
				Bookmark bookmark = bookmarks.FirstOrDefault(r => r.Record.LineNumber == lineNumber);

				if (bookmark != null)
				{
					Publish(bookmarks.Remove(bookmark));
					return true;
				}
				return false;
			}
		}

		// Called while holding the lock.
		private void Publish(ImmutableArray<Bookmark> bookmarks)
		{
			ImmutableInterlocked.InterlockedExchange(ref _bookmarks, bookmarks);
		}
	}
}
