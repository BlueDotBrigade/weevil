namespace BlueDotBrigade.Weevil
{
	using System.Collections.Immutable;
	using System.Threading;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Navigation;

	/// <summary>
	/// The mutable state of an open log, shared by every view of it, so records hold none (decision 6 of the #990 plan).
	/// </summary>
	/// <remarks>
	/// Each kind of state is a child store. One lock serializes every write to the session (decision 8), and reads never wait:
	/// each store publishes values that do not change, so filters read from many threads without locking.
	/// It lasts as long as the log is open: Clear and Reload keep it.
	/// </remarks>
	internal sealed class Session
	{
		private readonly object _writeLock = new object();
		private string _remarks = string.Empty;
		private TableOfContents _tableOfContents = new TableOfContents();

		/// <param name="lineCount">The number of lines expected; the comment and pin store grows when a later line is annotated.</param>
		public Session(int lineCount)
		{
			this.Annotations = new AnnotationStore(lineCount, _writeLock);
			this.Bookmarks = new BookmarkManager(_writeLock);
			this.Regions = new RegionManager(_writeLock);
			this.IncludeHistory = new FilterHistory(_writeLock);
			this.ExcludeHistory = new FilterHistory(_writeLock);
		}

		/// <summary>
		/// The comments and pins, by line number.
		/// </summary>
		public AnnotationStore Annotations { get; }

		public BookmarkManager Bookmarks { get; }

		public RegionManager Regions { get; }

		public FilterHistory IncludeHistory { get; }

		public FilterHistory ExcludeHistory { get; }

		/// <summary>
		/// The user's remarks about the log as a whole.
		/// </summary>
		public string Remarks
		{
			get => Volatile.Read(ref _remarks);
			set
			{
				lock (_writeLock)
				{
					Volatile.Write(ref _remarks, value ?? string.Empty);
				}
			}
		}

		public TableOfContents TableOfContents
		{
			get => Volatile.Read(ref _tableOfContents);
			set
			{
				lock (_writeLock)
				{
					Volatile.Write(ref _tableOfContents, value ?? new TableOfContents());
				}
			}
		}

		/// <summary>
		/// Replaces the remarks, table of contents, regions, bookmarks and filter histories with those read from the sidecar, in one write.
		/// </summary>
		public void Load(
			string remarks,
			TableOfContents tableOfContents,
			ImmutableArray<Region> regions,
			ImmutableArray<Bookmark> bookmarks,
			IEnumerable<string> includeHistory,
			IEnumerable<string> excludeHistory)
		{
			lock (_writeLock)
			{
				this.Remarks = remarks;
				this.TableOfContents = tableOfContents;
				this.Regions.Load(regions);
				this.Bookmarks.Load(bookmarks);
				this.IncludeHistory.Load(includeHistory);
				this.ExcludeHistory.Load(excludeHistory);
			}
		}
	}
}
