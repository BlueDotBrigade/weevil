namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;

	public interface IAnalyze
	{
		void UnpinAll();
		void RemoveAllFlags();
		void RemoveComments(bool clearAll);

		IList<IRecordCollectionAnalyzer> GetAnalyzers(ComponentType componentType);

		IRecordCollectionAnalyzer GetAnalyzer(AnalysisType analysisType);

		IRecordCollectionAnalyzer GetAnalyzer(string analyzerKey);
	}
}