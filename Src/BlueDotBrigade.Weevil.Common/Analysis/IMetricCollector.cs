namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using Data;

	/// <seealso cref="IRecordAnalyzer"/>
	public interface IMetricCollector
	{
		void Count(IRecord record);

		void Reset();

		IDictionary<string, object> GetResults();
	}
}
