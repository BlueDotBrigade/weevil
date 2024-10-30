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
		/// <returns></returns>
		IFilter ReApply();

		IFilter Apply(FilterType filterType, IFilterCriteria criteria);

		void Abort();

		event EventHandler<HistoryChangedEventArgs> HistoryChanged;
	}
}