namespace BlueDotBrigade.Weevil
{
	using System.Collections.Immutable;
	using Analysis;
	using Data;
	using Filter;
	using Navigation;
	using Reports;

	public interface ICoreEngine
	{
		IRecord this[int index] { get; }
		/// <summary>
		/// Represents all of the records that have been loaded from the <see cref="SourceFilePath"/>.
		/// </summary>
		/// <remarks>
		/// This property is intended to provide readonly access to the records via LINQ queries.
		///
		/// Coupling business logic to the <see cref="Records"/> property is strongly discouraged.
		/// </remarks>
		ImmutableArray<IRecord> Records { get; }
		LogFileMetrics Metrics { get; }
		IFilter Filter { get; }
		INavigate Navigator { get; }
		ISelect Selector { get; }
		IAnalyze Analyzer { get; }
		int Count { get; }
		bool IsSameAsDisk { get; }
		ContextDictionary Context { get; }
		string SourceFilePath { get; }
		/// <summary>
		/// Metadata related to the open file is persisted to disk.
		/// </summary>
		void Save();
		void Save(bool deleteSidecarBackup);
		void GenerateReport(ReportType report, string destinationFolder);
	}
}
