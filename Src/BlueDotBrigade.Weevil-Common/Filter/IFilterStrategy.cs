namespace BlueDotBrigade.Weevil.Filter
{
	using Data;

	public interface IFilterStrategy
	{
		FilterType FilterType { get; }

		IFilterCriteria FilterCriteria { get; }

		bool CanKeep(IRecord record);
	}
}
