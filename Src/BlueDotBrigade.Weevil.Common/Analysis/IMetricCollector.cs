namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using Data;

	public interface IMetricCollector
	{
		void Count(IRecord record);

		void Reset();

		IDictionary<string, object> GetResults();
	}
}
