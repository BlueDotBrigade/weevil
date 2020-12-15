namespace BlueDotBrigade.Weevil.Filter
{
	using System.Collections.Generic;

	public interface IFilterCriteria
	{
		IReadOnlyDictionary<string, object> Configuration { get; }
		string Exclude { get; }
		string Include { get; }
	}
}
