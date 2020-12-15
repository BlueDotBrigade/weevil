namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using Data;

	public interface IRecordAnalyzer
	{
		void Analyze(IRecord record);

		void Reset();

		IDictionary<string, object> GetResults();
	}
}
