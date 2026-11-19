namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;

	/// <remarks>
	/// Threading (#1248):
	/// <list type="bullet">
	/// <item>Call <see cref="Apply"/>, <see cref="ReApply"/> and <see cref="Abort"/> from any thread.</item>
	/// <item>One filter runs at a time. A new filter stops the one running, which throws <see cref="FilterAbortedException"/>, and waits for it to end.</item>
	/// <item>A filter is either stopped or shown. Only one that is not stopped changes the results, and raises <see cref="HistoryChanged"/> and <see cref="ResultsChanged"/> on its own thread, before <see cref="Apply"/> returns.</item>
	/// <item>Those handlers run during the filter's turn. A handler that applies a filter gets an <see cref="InvalidOperationException"/>. A handler must not wait for a thread that may be applying a filter, such as a UI thread that applies filters: each would wait for the other.</item>
	/// </list>
	/// </remarks>
	public interface IFilterBehaviors
	{
		/// <summary>
		/// The metrics of the results shown, such as the number of records of each severity. A stopped filter does not change them.
		/// </summary>
		IDictionary<string, object> GetMetrics();

		event EventHandler<ResultsChangedEventArgs> ResultsChanged;

		/// <summary>
		/// Re-applies the most recent filter to the in-memory records.
		/// </summary>
		/// <inheritdoc cref="Apply" path="/remarks"/>
		IFilter ReApply();

		/// <remarks>
		/// In disk mode, a read of the log that fails throws <see cref="IO.LogFileChangedException"/>, as every read of the log does (#1025, #1295).
		/// Unless the log is <see cref="IO.LogFileChangedException.IsUnreadable"/>, it changed and must be reloaded.
		/// </remarks>
		IFilter Apply(FilterType filterType, IFilterCriteria criteria);

		/// <summary>
		/// Stops the filter running, from the moment <see cref="Apply"/> is called. With none running, it does nothing.
		/// </summary>
		void Abort();

		event EventHandler<HistoryChangedEventArgs> HistoryChanged;
	}
}