namespace BlueDotBrigade.Weevil
{
	using System;
	using Data;
	using Filter;

	public interface IEngine : ICoreEngine
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
		/// Narrows the records kept in memory, using the rules of <paramref name="clearOperation"/>. The log file does not change.
		/// </summary>
		/// <remarks>
		/// The engine, its filter, selector and events, and the comments, pins and flags stay (#1018).
		/// The filter is reset to show every record kept, and the selected records that are kept stay selected.
		/// </remarks>
		void Clear(ClearOperation clearOperation);

		/// <summary>
		/// Reads the log file again, including any records a Clear removed.
		/// </summary>
		/// <remarks>
		/// The comments, pins, bookmarks, regions, remarks and filter history stay, and so do the filter and the selected records the log still holds.
		/// The previous records are released before the log is read, so memory never holds two copies (#1126).
		/// While it reads, and if reading fails, the engine shows an empty log.
		/// </remarks>
		void Reload();
	}
}