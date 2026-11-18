namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;

	/// <remarks>
	/// Threading (#1248):
	/// <list type="bullet">
	/// <item>Call <see cref="Apply"/>, <see cref="ReApply"/> and <see cref="Abort"/> from any thread.</item>
	/// <item>One filter runs at a time. A new filter stops the one running, which throws <see cref="FilterAbortedException"/>, and waits for it to end.</item>
	/// <item>Only a filter that is not stopped changes the results. It raises <see cref="ResultsChanged"/> on its own thread, before <see cref="Apply"/> returns.</item>
	/// <item>A <see cref="ResultsChanged"/> handler must not apply a filter: it would wait for itself.</item>
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
		/// In disk mode, a read of the log that fails throws its <see cref="System.IO.IOException"/> or <see cref="UnauthorizedAccessException"/> as it is,
		/// for example <see cref="IO.LogFileChangedException"/> when the log changed: the log must then be reloaded (#1025).
		/// </remarks>
		IFilter Apply(FilterType filterType, IFilterCriteria criteria);

		/// <summary>
		/// Stops the filter running, from the moment <see cref="Apply"/> is called. With none running, it does nothing.
		/// </summary>
		void Abort();

		event EventHandler<HistoryChangedEventArgs> HistoryChanged;
	}
}