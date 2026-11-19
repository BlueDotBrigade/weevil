namespace BlueDotBrigade.Weevil
{
	using Analysis;
	using Data;
	using Filter;
	using Navigation;
	using Reports;

	public interface ICoreEngine
	{
		IRecord this[int index] { get; }
		/// <summary>
		/// Represents the records loaded from the <see cref="SourceFilePath"/> that the engine keeps: all of them, until a Clear removes some.
		/// </summary>
		/// <remarks>
		/// This property is intended to provide readonly access to the records via LINQ queries.
		///
		/// Coupling business logic to the <see cref="Records"/> property is strongly discouraged.
		/// </remarks>
		RecordList Records { get; }

		LogFileMetrics Metrics { get; }

		IFilter Filter { get; }
		INavigate Navigate { get; }
		ISelect Selector { get; }
		IAnalyze Analyzer { get; }
		IRegionManager Regions { get; }

		IBookmarkManager Bookmarks { get; }

		int Count { get; }

		/// <summary>
		/// Returns <see langword="True"/> when at least one clear operation
		/// has been performed since the records were loaded from the <see cref="SourceFilePath"/>.
		/// </summary>
		bool HasBeenCleared { get; }

		ContextDictionary Context { get; }

		/// <summary>
		/// Represents user remarks that are applicable to the entire source file.
		/// </summary>
		string SourceFileRemarks { get; set; }

		/// <summary>
		/// The log as the analyst knows it. For a log read from a copy, such as one extracted from a zip, a zip reads like a folder:
		/// <c>C:\Logs\Archive.zip\App.log</c> (#1312). It need not be a file that exists: read the log through the engine, and save beside it in <see cref="SourceDirectory"/>.
		/// </summary>
		string SourceFilePath { get; }

		/// <summary>
		/// The folder of the file Weevil reads, where exports, reports and splits are saved: for a log read from a copy, the copy's folder (#1253).
		/// </summary>
		string SourceDirectory { get; }
		/// <summary>
		/// Metadata related to the open file is persisted to disk.
		/// </summary>
		void Save();
		void Save(bool deleteSidecarBackup);
		void GenerateReport(ReportType report, string destinationFolder);
	}
}
