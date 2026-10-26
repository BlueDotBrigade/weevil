namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;

	public interface IAnalyze
	{
		void UnpinAll();

		void RemoveAllFlags();

		/// <summary>
		/// Unflags every line, then flags the given lines whose records are in memory.
		/// </summary>
		/// <returns>The number of lines flagged, each counted once.</returns>
		int FlagOnly(IEnumerable<int> lineNumbers);

		void RemoveComments(bool clearAll);

		IList<IRecordAnalyzer> GetAnalyzers(ComponentType componentType);

		ImmutableArray<IInsight> GetInsights();

		void Analyze(AnalysisType analysisType);

		Results Analyze(AnalysisType analysisType, IUserDialog userDialog);

		Results Analyze(string analyzerKey, IUserDialog userDialog);
	}
}