namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Threading;
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

		/// <summary>
		/// Runs the analyzer, and stops it when <paramref name="cancellationToken"/> is cancelled, as Abort does (#1027).
		/// </summary>
		/// <remarks>
		/// An analyzer that stops keeps the comments, pins and flags it changed so far.
		/// </remarks>
		/// <exception cref="System.OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
		Results Analyze(AnalysisType analysisType, IUserDialog userDialog, CancellationToken cancellationToken);

		/// <inheritdoc cref="Analyze(AnalysisType, IUserDialog, CancellationToken)"/>
		Results Analyze(string analyzerKey, IUserDialog userDialog, CancellationToken cancellationToken);
	}
}