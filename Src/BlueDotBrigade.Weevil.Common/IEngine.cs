namespace BlueDotBrigade.Weevil
{
	using System;
	using Data;
	using Filter;

	public interface IEngine : ICoreEngine
	{
		/// <summary>
		/// Raised when the filter's results change. Subscribe once per open log: unlike <see cref="ICoreEngine.Filter"/>'s own event,
		/// it keeps working after <see cref="Clear"/> and <see cref="Reload"/> replace the filter.
		/// </summary>
		/// <remarks>
		/// The sender is the filter.
		/// <see cref="Clear"/> does not raise it: Clear resets the filter, so read <see cref="IFilter.Results"/> or apply a filter afterwards.
		/// <see cref="Reload"/> raises it once, when it re-applies the current filter.
		/// </remarks>
		event EventHandler<ResultsChangedEventArgs> FilterResultsChanged;

		/// <summary>
		/// Raised when the filter's include or exclude history changes. Subscribe once per open log: unlike <see cref="ICoreEngine.Filter"/>'s own event,
		/// it keeps working after <see cref="Clear"/> and <see cref="Reload"/> replace the filter.
		/// </summary>
		/// <remarks>
		/// The sender is the history list that changed: <see cref="IFilterTraits.IncludeHistory"/> or <see cref="IFilterTraits.ExcludeHistory"/>.
		/// <see cref="Clear"/> does not raise it. <see cref="Clear"/> and <see cref="Reload"/> replace both lists, so read them again after calling either method.
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

		void Clear(ClearOperation clearOperation);

		void Reload();
	}
}