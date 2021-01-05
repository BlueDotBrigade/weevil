namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;

	public interface IAnalyze
	{
		void UnpinAll();
		void RemoveAllFlags();
		void RemoveComments(bool clearAll);

		IList<IRecordAnalyzer> GetAnalyzers(ComponentType componentType);

		IRecordAnalyzer GetAnalyzer(AnalysisType analysisType);

		IRecordAnalyzer GetAnalyzer(string analyzerKey);
	}
}