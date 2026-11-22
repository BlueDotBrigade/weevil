namespace BlueDotBrigade.Weevil
{
	using System;
	using Data;
	using Filter;
	using IO;

	/// <remarks>
	/// <see cref="Reload"/> replaces the engine's filter, selector, navigation, analyzer and records: read them from the engine each time, rather than keeping them.
	/// A record, or a <see cref="RecordList"/>, kept after another log opens or after Reload still reads its own log.
	/// In disk mode that read throws <see cref="IO.LogFileChangedException"/> once the log changed or is gone (#1025, #1295).
	///
	/// The engine locks its log, so another instance of Weevil, or another engine, doesn't open it (#1254). <see cref="IDisposable.Dispose"/> releases the lock, and nothing else:
	/// the engine still reads its records. So save first, then dispose the engine once the log is closed or replaced. <see cref="Clear"/> and <see cref="Reload"/> keep the lock.
	/// </remarks>
	public interface IEngine : ICoreEngine, IDisposable
	{
		/// <summary>
		/// Raised when the filter's results change. Subscribe once per open log: unlike <see cref="ICoreEngine.Filter"/>'s own event,
		/// it keeps working after <see cref="Reload"/> replaces the filter. <see cref="Clear"/> keeps the filter.
		/// </summary>
		/// <remarks>
		/// The sender is the filter.
		/// <see cref="Clear"/> does not raise it: Clear resets the filter, so read <see cref="IFilter.Results"/> or apply a filter afterwards.
		/// <see cref="Reload"/> raises it once, when it re-applies the current filter.
		/// </remarks>
		event EventHandler<ResultsChangedEventArgs> FilterResultsChanged;

		/// <summary>
		/// Raised when the filter's include or exclude history changes. Subscribe once per open log: unlike <see cref="ICoreEngine.Filter"/>'s own event,
		/// it keeps working after <see cref="Reload"/> replaces the filter. <see cref="Clear"/> keeps the filter.
		/// </summary>
		/// <remarks>
		/// The sender is the history list that changed: <see cref="IFilterTraits.IncludeHistory"/> or <see cref="IFilterTraits.ExcludeHistory"/>.
		/// <see cref="Clear"/> does not raise it. <see cref="Clear"/> and <see cref="Reload"/> keep both lists, because they belong to the log (#1109).
		/// </remarks>
		event EventHandler<HistoryChangedEventArgs> FilterHistoryChanged;

		/// <summary>
		/// The comments and pins of the open log. The same object for the log's lifetime: <see cref="Clear"/> and <see cref="Reload"/> keep it.
		/// </summary>
		IAnnotations Annotations { get; }

		/// <summary>
		/// Gets and sets a record's comment, pin and flag, and gets its elapsed time. The same object for the log's lifetime.
		/// </summary>
		/// <remarks>
		/// Records hold no state of their own (decision 7 of the #990 plan). Use <see cref="Annotations"/> to be told when comments and pins change.
		/// </remarks>
		IRecordState RecordState { get; }

		/// <summary>
		/// Whether the log's content is kept in RAM or read back from the log (#421). <see cref="Clear"/> and <see cref="Reload"/> keep it.
		/// </summary>
		StorageMode StorageMode { get; }

		/// <summary>
		/// The part of the log that is open: the whole log, one section, one region, or a range of lines (#1219). <see cref="Clear"/> keeps it, and <see cref="Reload"/> opens the same part again.
		/// </summary>
		LogPart Part { get; }

		/// <summary>
		/// Narrows the records kept in memory, using the rules of <paramref name="clearOperation"/>. The log file does not change.
		/// </summary>
		/// <remarks>
		/// The engine, its filter, selector and events, and the comments, pins and flags stay (#1018).
		/// The filter is reset to show every record kept, and the selected records that are kept stay selected.
		/// It saves the sidecar first, so the comments and pins on the records it clears are kept (#1004). When the sidecar can never be written,
		/// as in a read-only folder, it logs a warning and clears the records anyway, so their unsaved comments and pins are not saved (#1246).
		/// Any other failure to save, such as a locked sidecar, stops Clear, and nothing is cleared.
		/// </remarks>
		void Clear(ClearOperation clearOperation);

		/// <summary>
		/// Reads the log file again, including any records a Clear removed.
		/// </summary>
		/// <remarks>
		/// It opens the same <see cref="Part"/> of the log as it is now (#1219): a section or region found again by its name among the log's sections and regions,
		/// which may have changed since it was opened, lines from a start line to the end of the log, so appended lines appear, or the same range of lines.
		/// When the section or region is no longer there, it throws a <see cref="System.Collections.Generic.KeyNotFoundException"/> before it lets go of the records, so they stay.
		///
		/// The comments, pins, bookmarks, regions, remarks and filter history stay, and so do the filter, the storage mode and the selected records the log still holds.
		/// The engine lets go of the previous records before it reads the log (#1126). While it reads, and if reading fails, it shows the same log without records, with the same filter.
		/// So save first: after a failed read, saving keeps only the comments and pins already saved.
		/// A failed read does not raise <see cref="FilterResultsChanged"/>, so read <see cref="IFilter.Results"/> afterwards.
		/// </remarks>
		void Reload();
	}
}