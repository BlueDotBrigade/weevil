namespace BlueDotBrigade.Weevil.Analysis
{
	public interface IAnalyze
	{
		void UnpinAll();
		void RemoveAllFlags();
		void RemoveComments(bool clearAll);

		IRecordCollectionAnalyzer GetAnalyzer(AnalysisType analysisType);
	}
}