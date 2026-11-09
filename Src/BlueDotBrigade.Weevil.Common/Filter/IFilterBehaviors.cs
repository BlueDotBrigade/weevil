namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;

	public interface IFilterBehaviors
	{
		IDictionary<string, object> GetMetrics();

		event EventHandler<ResultsChangedEventArgs> ResultsChanged;

		/// <summary>
		/// Re-applies the most recent filter to the in-memory records.
		/// </summary>
		/// <remarks>
		/// In disk mode, a read of the log that fails throws its <see cref="System.IO.IOException"/> as it is,
		/// for example <see cref="IO.LogFileChangedException"/> when the log changed: the log must then be reloaded (#1025).
		/// </remarks>
		IFilter ReApply();

		/// <remarks>
		/// In disk mode, a read of the log that fails throws its <see cref="System.IO.IOException"/> as it is,
		/// for example <see cref="IO.LogFileChangedException"/> when the log changed: the log must then be reloaded (#1025).
		/// </remarks>
		IFilter Apply(FilterType filterType, IFilterCriteria criteria);

		void Abort();

		event EventHandler<HistoryChangedEventArgs> HistoryChanged;
	}
}