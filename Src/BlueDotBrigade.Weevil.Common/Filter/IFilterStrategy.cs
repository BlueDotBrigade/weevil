namespace BlueDotBrigade.Weevil.Filter
{
	using Data;

	public interface IFilterStrategy
	{
		FilterType FilterType { get; }

		IFilterCriteria FilterCriteria { get; }

		/// <param name="record">The record to test.</param>
		/// <param name="content">Holds the record's content, so it is read once for every expression (#1131).</param>
		bool CanKeep(IRecord record, ContentBuffer content);
	}
}
