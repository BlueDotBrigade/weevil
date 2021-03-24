﻿namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;

	public interface IAnalyze
	{
		void UnpinAll();

		void RemoveAllFlags();
		void RemoveComments(bool clearAll);

		IList<IRecordAnalyzer> GetAnalyzers(ComponentType componentType);

		ImmutableArray<IInsight> GetInsights();

		void Analyze(AnalysisType analysisType);

		void Analyze(AnalysisType analysisType, IUserDialog userDialog);

		void Analyze(string analyzerKey, IUserDialog userDialog);
	}
}