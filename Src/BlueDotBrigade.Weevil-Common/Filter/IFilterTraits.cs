namespace BlueDotBrigade.Weevil.Filter
{
	using System.Collections.Generic;

	public interface IFilterTraits
	{
		IList<string> IncludeHistory { get; }
		IList<string> ExcludeHistory { get; }
		FilterType FilterType { get; }
		IFilterCriteria Criteria { get; }
	}
}
