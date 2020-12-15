namespace BlueDotBrigade.Weevil
{
	using Analysis;

	public interface IAnalysisFactory
	{
		IRecordCollectionAnalyzer Create(AnalysisType analysisType);
	}
}