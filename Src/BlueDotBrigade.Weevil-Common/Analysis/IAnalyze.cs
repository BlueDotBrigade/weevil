namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.IO;

	public interface IAnalyze
	{
		void UnpinAll();

		void RemoveAllFlags();
		void RemoveComments(bool clearAll);

		IList<IRecordAnalyzer> GetAnalyzers(ComponentType componentType);

		void Analyze(AnalysisType analysisType, IUserDialog userDialog);

		void Analyze(string analyzerKey, IUserDialog userDialog);
	}
}