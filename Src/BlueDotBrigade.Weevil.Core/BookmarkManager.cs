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
		private int _nextSequenceNumber;

		internal BookmarkManager() : this(ImmutableArray<Bookmark>.Empty)
		{
			// nothing to do
		}

		internal BookmarkManager(ImmutableArray<Bookmark> bookmarks)
		{
			_bookmarks = new List<Bookmark>(bookmarks);
			_bookmarkPadlock = new object();
			_nextSequenceNumber = CalculateNextSequenceNumber(bookmarks);
		}

		private static int CalculateNextSequenceNumber(ImmutableArray<Bookmark> bookmarks)
		{
			// Find the highest numeric bookmark name and start sequence from there
			int maxSequence = 0;
			foreach (var bookmark in bookmarks)
			{
				if (int.TryParse(bookmark.Name, out int sequenceNumber))
				{
					if (sequenceNumber > maxSequence)
					{
						maxSequence = sequenceNumber;
					}
				}
			}
			return maxSequence + 1;
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
				// If no name provided, use the next sequential number
				var effectiveName = string.IsNullOrEmpty(bookmarkName) 
					? _nextSequenceNumber.ToString() 
					: bookmarkName;

				var bookmark = new Bookmark(effectiveName, lineNumber);

				if (_bookmarks.Any(r => r.Record.LineNumber == bookmark.Record.LineNumber))
				{
					throw new InvalidOperationException("Unable to create bookmark because this bookmark has already been defined.");
				}

				_bookmarks.Add(bookmark);

				// Increment sequence number if we used it
				if (string.IsNullOrEmpty(bookmarkName))
				{
					_nextSequenceNumber++;
				}
			}
		}

		public bool TryGetBookmarkName(int lineNumber, out string bookmarkName)
		{			
			lock (_bookmarkPadlock)
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
		public bool Contains(int lineNumber)
		{
			lock (_bookmarkPadlock)
			{
				return _bookmarks.Any(r => r.Record.LineNumber == lineNumber);
			}
		}

		public void Clear()
		{
			lock (_bookmarkPadlock)
			{
				_bookmarks.Clear();
				_nextSequenceNumber = 1;  // Reset sequence counter when clearing all bookmarks
			}
		}

		public bool Clear(int lineNumber)
		{
			lock (_bookmarkPadlock)
			{
				Bookmark bookmark = _bookmarks.FirstOrDefault(r => r.Record.LineNumber == lineNumber);

				if (bookmark != null)
				{
					_bookmarks.Remove(bookmark);
					// Recalculate sequence number based on remaining bookmarks
					_nextSequenceNumber = CalculateNextSequenceNumber(_bookmarks.ToImmutableArray());
					return true;
				}
				return false;
			}
		}
	}
}
