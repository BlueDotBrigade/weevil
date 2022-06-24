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
		INavigate Navigate { get; }
		ISelect Selector { get; }
		IAnalyze Analyzer { get; }
		int Count { get; }
		/// <summary>
		/// Returns <see langword="True"/> when at least one clear operation
		/// has been performed since the records were loaded from the <see cref="SourceFilePath"/>.
		/// </summary>
		bool HasBeenCleared { get; }
		ContextDictionary Context { get; }

		string UserRemarks { get; set; }
		string SourceFilePath { get; }
		string SourceDirectory { get; }
		/// <summary>
		/// Metadata related to the open file is persisted to disk.
		/// </summary>
		void Save();
		void Save(bool deleteSidecarBackup);
		void GenerateReport(ReportType report, string destinationFolder);
	}
}
