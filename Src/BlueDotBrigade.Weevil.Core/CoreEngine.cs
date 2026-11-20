namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Threading;
	using Analysis;
	using Configuration.Sidecar;
	using Data;
	using Diagnostics;
	using Filter;
	using IO;
	using Navigation;
	using Reports;

	[DebuggerDisplay("InstanceId={_instanceId}, Records={Count}, Results={Filter.Results.Count}")]
	internal partial class CoreEngine : ICoreEngine
	{
		#region Fields
		private readonly ICoreExtension _coreExtension;

		private readonly ITelemetryMetricRecorder _telemetryRecorder;

		// The file read; for a log read from a copy, the copy (#1312).
		private readonly string _sourceFilePath;

		// The log as the analyst knows it, when the file read is a copy: otherwise null.
		private readonly string _displayPath;
		private readonly Encoding _sourceFileEncoding;
		private readonly ContextDictionary _context;
		private readonly StorageMode _storageMode;
		private readonly LogFileSource _source;

		private LogFileMetrics _logFileMetrics;

		// The records this view keeps: Clear narrows them (#1018).
		private readonly RecordScope _scope;

		// The session's state is shared by every view of the log; flags and elapsed times belong to the view (#1013, #1107).
		private readonly Session _session;
		private readonly RecordState _recordState;

		private readonly FilterManager _filterManager;
		private readonly SidecarManager _sidecarManager;
		private readonly LineIndex _lineIndex;
		private readonly SelectionManager _selectionManager;
		private readonly NavigationManager _navigationManager;
		private readonly IAnalyze _analysisManager;

		private volatile bool _hasBeenCleared;

		private static int _instancesCreated;
		private readonly int _instanceId;
		#endregion

		#region Object Lifetime

		/// <summary>
		/// Facilitates the creation of a new <see cref="CoreEngine"/> object using a Fluent API.
		/// </summary>
		/// <param name="path">A path to the file that will be opened.</param>
		internal static CoreEngineBuilder FromPath(string path)
		{
			return new CoreEngineBuilder(path);
		}

		/// <summary>
		/// Whether the records keep their content, or read it back from the log. Clear and Reload keep it (#1025).
		/// </summary>
		internal StorageMode StorageMode => _storageMode;

		/// <summary>
		/// The file Weevil read and the checksum of each block, in both modes (#1255). On disk, the records read their content back through it,
		/// and it says whether the log changed since it was read (#1025). The mode is <see cref="StorageMode"/>, never whether this is null.
		/// </summary>
		/// <remarks>
		/// <see langword="null"/> for the engine without records that Reload uses.
		/// </remarks>
		internal LogFileSource Source => _source;

		/// <summary>
		/// Creates an instance of <see cref="CoreEngine"/> which can only be instantiated through a Fluent API managed by <see cref="CoreEngineBuilder"/>.
		/// </summary>
		/// <param name="storageMode">Whether the records keep their content, or read it back from the log (#1025).</param>
		/// <param name="hasReadLog"><see langword="false"/> for the engine without records that Reload uses while it reads the log again (#1126).</param>
		private CoreEngine(
			string sourceFilePath,
			long sourceFileLength,
			Encoding sourceFileEncoding,
			TimeSpan sourceFileLoadingPeriod,
			ICoreExtension coreExtension,
			ContextDictionary context,
			SidecarManager sidecarManager,
			LineIndex lineIndex,
			RecordList records,
			Session session,
			RecordState recordState,
			ITelemetryMetricRecorder telemetryRecorder,
			StorageMode storageMode,
			LogFileSource source,
			string displayPath,
			bool hasReadLog)
		{
			_instanceId = Interlocked.Increment(ref _instancesCreated);

			_telemetryRecorder = telemetryRecorder ?? NullTelemetryMetricRecorder.Instance;

			if (string.IsNullOrWhiteSpace(sourceFilePath))
			{
				throw new ArgumentException("A valid file path was expected.", nameof(sourceFilePath));
			}

			LogCoreEngineVersion();

			_coreExtension = coreExtension;
			_context = context;
			_storageMode = storageMode;
			_source = source;

			_sourceFilePath = sourceFilePath;
			_displayPath = displayPath;
			_sourceFileEncoding = sourceFileEncoding;

			_scope = new RecordScope(records);
			_session = session;
			_recordState = recordState;

			var recordAndMetadataLoadingStopwatch = Stopwatch.StartNew();

			// An engine without records has read nothing (#1126). CoreEngineBuilder.Build collects what reading left behind (#969, #1240).
			if (hasReadLog)
			{
				Log.Default.Write(
					LogSeverityType.Debug,
					"File loading...");

				Log.Default.Write(
					LogSeverityType.Information,
					"File loading has retrieved records from disk.",
					new Dictionary<string, object>
					{
						{ "SourceFileLoadingPeriod", sourceFileLoadingPeriod.TotalSeconds},
						{ "RecordCount", records.Count},
						{ "FileSize", sourceFileLength},
						{ "SourceFilePath", _sourceFilePath},
					});
			}

			_sidecarManager = sidecarManager;
			_lineIndex = lineIndex;

			_analysisManager = new AnalysisManager(this, _coreExtension, _telemetryRecorder);

			IDictionary<string, string> filterAliases = _coreExtension.GetFilterAliases(_context);
			var filterAliasExpander = new FilterAliasExpander(filterAliases);

			_filterManager = new FilterManager(
				_coreExtension,
				_context,
				filterAliasExpander,
				_scope,
				GetRecordCounters(),
				_session.Regions,
				_session.Bookmarks,
				_recordState,
				_session.IncludeHistory,
				_session.ExcludeHistory,
				_telemetryRecorder,
				_storageMode,
				_source);

			// Construction-time default filter: applied without recording telemetry (not a user action).
			_filterManager.ApplyInternal(FilterType.PlainText, FilterCriteria.None);
			_filterManager.ResultsChanged += OnResultsChanged;

			_navigationManager = new NavigationManager(
				_sourceFilePath,
				_coreExtension,
				records,
				_session,
				_recordState,
				_telemetryRecorder);

			_selectionManager = new SelectionManager(
				_scope,
				_filterManager.Results,
				_navigationManager, // TODO: adding navigation manager is a code smell
				_sourceFileEncoding,
				_recordState,
				new Action(() => { _filterManager.ReApply(); }));

			recordAndMetadataLoadingStopwatch.Stop();

			_logFileMetrics = new LogFileMetrics(
				sourceFileLength,
				records.Count,
				sourceFileLoadingPeriod,
				recordAndMetadataLoadingStopwatch.Elapsed);

			Debug.WriteLine($"{nameof(CoreEngine)} resources have been allocated.");
		}

		/// <summary>
		/// Says whether this engine read the same log as another: the same length and number of lines.
		/// A log that changed or grew in between differs, so what was kept by line number no longer applies (#1224).
		/// </summary>
		/// <remarks>
		/// A log rewritten with the same length and number of lines is not told apart.
		/// </remarks>
		internal bool HasReadSameLogAs(CoreEngine other)
		{
			return other is not null &&
			       _logFileMetrics.FileSize == other._logFileMetrics.FileSize &&
			       _lineIndex.LineCount == other._lineIndex.LineCount;
		}

		/// <summary>
		/// Returns an engine for the same log, with the same session, flags and filter, that holds none of the records.
		/// Reload uses it while it reads the log again, so the engine holds none of the previous records meanwhile (#1126).
		/// </summary>
		/// <remarks>
		/// If reading fails, the engine stays, so it still saves this log's sidecar, and a retry reloads this log.
		/// </remarks>
		internal CoreEngine WithoutRecords()
		{
			// Without the records they were calculated from, which this engine lets go of (#1126, #1224).
			return WithoutRecords(_filterManager.FilterType, _filterManager.Criteria, _filterManager.ComparedElapsedTimes.WithoutRecords());
		}

		/// <summary>
		/// Returns an engine for the same log, with the same session and flags, that holds none of the records and shows them with the given filter.
		/// </summary>
		/// <remarks>
		/// Reload uses it when the filter it applies again cannot read the log, so a retry keeps that filter (#1025).
		/// </remarks>
		/// <param name="comparedElapsedTimes">
		/// The elapsed times the filter compared, which applying it again compares too (#1224), or <see langword="null"/> for those of the results shown.
		/// </param>
		internal CoreEngine WithoutRecords(FilterType filterType, IFilterCriteria criteria, ElapsedTimes comparedElapsedTimes)
		{
			var empty = new CoreEngine(
				_sourceFilePath,
				_logFileMetrics.FileSize,
				_sourceFileEncoding,
				TimeSpan.Zero,
				_coreExtension,
				_context,
				_sidecarManager,
				_lineIndex,
				RecordList.Empty,
				_session,
				_recordState,
				_telemetryRecorder,
				_storageMode,
				source: null,
				_displayPath,
				hasReadLog: false);

			// Not a user action, so no telemetry, as when the engine was created.
			empty._filterManager.ApplyInternal(filterType, criteria, comparedElapsedTimes);

			return empty;
		}

		/// <summary>
		/// Determines the current version of the core engine, and records the value in the application's log file.
		/// </summary>
		/// <seealso href="https://developercommunity.visualstudio.com/content/problem/315285/assemblygetentryassembly-returns-null-in-unit-test.html">MSDN: Assembly.GetEntryAssembly() returns null in unit test projects</seealso>
		private void LogCoreEngineVersion()
		{
			if (Assembly.GetEntryAssembly() == null)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					$"The core engine has been created. Unable to determine the version - a unit test may be executing.");
			}
			else
			{
				Version version = Assembly.GetEntryAssembly().GetName().Version;
				Log.Default.Write($"The core engine has been created. Version={version}");
			}
		}

		#endregion

		#region Properties
		public IRecord this[int index] => _scope.Records[index];

		public LogFileMetrics Metrics => _logFileMetrics;

		public FilterManager Filter => _filterManager;

		internal Session Session => _session;

		/// <summary>
		/// The state of the records this view shows: the session's comments and pins, and the view's flags and elapsed times (decision 7 of the #990 plan).
		/// </summary>
		internal RecordState RecordState => _recordState;

		public NavigationManager Navigate => _navigationManager;

		public SelectionManager Selector => _selectionManager;

		public IAnalyze Analyzer => _analysisManager;

		public IRegionManager Regions => _session.Regions;

		public IBookmarkManager Bookmarks => _session.Bookmarks;

		public RecordList Records => _scope.Records;

		public int Count => _scope.Records.Count;

		public bool HasBeenCleared => _hasBeenCleared;

		public ContextDictionary Context => _context;

		public string SourceFileRemarks
		{
			get { return _session.Remarks; }
			set { _session.Remarks = value; }
		}

		/// <summary>
		/// The log as the analyst knows it: for a log read from a copy, such as one extracted from a zip, the path given by
		/// <see cref="CoreEngineBuilder.UsingDisplayPath"/>, otherwise the file read (#1312).
		/// </summary>
		public string SourceFilePath => _displayPath ?? _sourceFilePath;

		/// <summary>
		/// The folder of the file read, where exports, reports and splits are saved: for a log read from a copy, the copy's folder (#1253, #1312).
		/// </summary>
		public string SourceDirectory => Path.GetDirectoryName(_sourceFilePath);

		/// <summary>
		/// The file read, which <see cref="SourceFilePath"/> names as the analyst knows it.
		/// </summary>
		internal string FilePath => _sourceFilePath;

		/// <summary>
		/// The path given by <see cref="CoreEngineBuilder.UsingDisplayPath"/>, which Reload keeps: otherwise <see langword="null"/>.
		/// </summary>
		internal string DisplayPath => _displayPath;

		public int InstanceId => _instanceId;
		#endregion

		#region Expose ICoreEngine interface
		/*
		 * Within this assembly, we have complete access to the various managers.
		 *
		 * Externally, we only need to expose what is required by the `ICoreEngine` contract.
		 *
		 * If a core engine member does not match the interface type,
		 * then we have to explicitly expose the matching type.
		 */

		IFilter ICoreEngine.Filter => this.Filter;

		INavigate ICoreEngine.Navigate => this.Navigate;

		ISelect ICoreEngine.Selector => this.Selector;

		#endregion

		#region Private Methods

		private ImmutableArray<IMetricCollector> GetRecordCounters()
		{
			return _coreExtension
				.GetRecordCounters(_context)
				.ToImmutableArray();
		}

		/// <summary>
		/// Narrows the records this view keeps, using the rules of <paramref name="clearOperation"/> (#1018).
		/// The engine, its filter, its events and the session stay.
		/// </summary>
		/// <remarks>
		/// As when Clear built a new engine: the filter is reset, the selected records that are kept stay selected,
		/// and the record count and the filter's metrics are recounted. The caller saves the sidecar first (#1004).
		/// </remarks>
		internal void Clear(ClearOperation clearOperation)
		{
			// Only once Narrow returns do the view, its filter and its selection hold nothing but the records kept,
			// so Clear's forced collection frees the records cleared, and the chunks only they were read from, at once (#1022).
			// Narrow reads the records, so this method holds none, even in a Debug build, which keeps a method's temporaries until it returns.
			if (Narrow(clearOperation))
			{
				// Aggressive, not Forced: Forced keeps the freed memory committed, so the working set barely falls.
				// Aggressive returns it to the operating system, as opening does (#969).
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
			}
		}

		/// <returns><see langword="true"/> if records were cleared.</returns>
		private bool Narrow(ClearOperation clearOperation)
		{
			RecordList records = _scope.Records;
			RecordList selected = _selectionManager.GetSelected();

			RecordList kept = new InMemoryRecordRepository(
					records,
					_filterManager.Results,
					selected,
					clearOperation,
					_session.Regions.Regions)
				.Get(int.MaxValue);

			// The selection and the active record are set for the kept records before the filter shows them,
			// so listeners read the state that matches the new results (#1015). As with a new engine, the active record
			// is the last selected record kept, or none.
			IRecord lastSelectedKept = _selectionManager.Narrow(selected, kept);
			_navigationManager.Reset();
			_navigationManager.UpdateDataSource(kept);
			if (Record.IsGenuine(lastSelectedKept))
			{
				_navigationManager.SetActiveLineNumber(lastSelectedKept.LineNumber);
			}

			_scope.Narrow(kept);
			_hasBeenCleared = true;
			_logFileMetrics = new LogFileMetrics(
				_logFileMetrics.FileSize,
				kept.Count,
				_logFileMetrics.SourceFileLoadingPeriod,
				_logFileMetrics.RecordAndMetadataLoadDuration);

			// Not a user action, so no telemetry, as when the engine was created.
			// The filter has no criteria, so @Elapsed reads nothing. It is given no elapsed times: those of the results shown
			// before the Clear hold those results, and so the record table the Clear frees, until the next filter (#1150).
			_filterManager.ApplyInternal(FilterType.PlainText, FilterCriteria.None, ElapsedTimes.None);

			Log.Default.Write(
				LogSeverityType.Information,
				"Records have been cleared.",
				new Dictionary<string, object>
				{
					{ "ClearOperation", clearOperation },
					{ "RecordsKept", kept.Count },
					{ "SelectedRecordsKept", _selectionManager.SelectedCount },
				});

			return kept.Count < records.Count;
		}

		private void OnResultsChanged(object sender, ResultsChangedEventArgs e)
		{
			// Before anyone else hears of the new results, so they read the selection that matches them (#1015).
			var stopwatch = Stopwatch.StartNew();
			_selectionManager.KeepRecordsShownBy(e.Records);
			_navigationManager.UpdateDataSource(e.Records);

			Log.Default.Write(
				LogSeverityType.Debug,
				$"The selection was updated to the filter's results. TimeElapsed={stopwatch.Elapsed}");
		}
		#endregion

		#region Event Handlers
		#endregion

		public void Save()
		{
			Save(false);
		}

		public void Save(bool deleteBackup)
		{
			using FileStream log = TryOpenSourceFile();

			var sidecarData = new SidecarData
			{
				Records = _scope.Records,
				Annotations = _session.Annotations,
				Context = _context,
				FilterTraits = _filterManager,
				SourceFileRemarks = _session.Remarks,
				TableOfContents = _session.TableOfContents,
				Regions = _session.Regions.Regions,
				Bookmarks = _session.Bookmarks.Bookmarks,
				ByteOffsetOfLine = lineNumber => GetByteOffset(log, lineNumber),
			};

			_sidecarManager.Save(sidecarData, deleteBackup);
		}

		/// <summary>
		/// Opens the log so that byte offsets can be taken from the line index, or returns <see langword="null"/> if it is gone.
		/// </summary>
		private FileStream TryOpenSourceFile()
		{
			try
			{
				return System.IO.File.Exists(_sourceFilePath) ? FileHelper.Open(_sourceFilePath) : null;
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				Log.Default.Write(LogSeverityType.Warning, exception, $"The log could not be opened, so the sidecar will not include byte offsets. SourceFilePath={_sourceFilePath}");
				return null;
			}
		}

		private long GetByteOffset(Stream log, int lineNumber)
		{
			return log != null && lineNumber >= 1 && lineNumber <= _lineIndex.LineCount
				? _lineIndex.GetByteOffset(log, lineNumber)
				: -1;
		}

		public void GenerateReport(ReportType report, string destinationFolder)
		{
			if (report == ReportType.CommentSummary)
			{
				RecordList filterResults = _filterManager.Results;
				new CommentSummaryReport(filterResults, _recordState).Generate(destinationFolder);
			}
		}
	}
}