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

	[DebuggerDisplay("InstanceId={_instanceId}, Records={Count}, Results={Filter.Results.Length}")]
	internal partial class CoreEngine : ICoreEngine
	{
		#region Fields
		private readonly ICoreExtension _coreExtension;

		private readonly ITelemetryMetricRecorder _telemetryRecorder;

		private readonly string _sourceFilePath;
		private readonly Encoding _sourceFileEncoding;
		private readonly ContextDictionary _context;

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
		/// Creates an instance of <see cref="CoreEngine"/> which can only be instantiated through a Fluent API managed by <see cref="CoreEngineBuilder"/>.
		/// </summary>
		private CoreEngine(
			string sourceFilePath,
			long sourceFileLength,
			Encoding sourceFileEncoding,
			TimeSpan sourceFileLoadingPeriod,
			ICoreExtension coreExtension,
			ContextDictionary context,
			SidecarManager sidecarManager,
			LineIndex lineIndex,
			ImmutableArray<IRecord> records,
			Session session,
			RecordState recordState,
			ITelemetryMetricRecorder telemetryRecorder)
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

			_sourceFilePath = sourceFilePath;
			_sourceFileEncoding = sourceFileEncoding;

			_scope = new RecordScope(records);
			_session = session;
			_recordState = recordState;

			Log.Default.Write(
				LogSeverityType.Debug,
				"File loading...");

			var recordAndMetadataLoadingStopwatch = Stopwatch.StartNew();

			// The only forced collection while opening a file: it frees the memory used while parsing,
			// and Aggressive mode returns that memory to the operating system, so memory right after opening stays low (#969).
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);

			Log.Default.Write(
				LogSeverityType.Information,
				"File loading has retrieved records from disk.",
				new Dictionary<string, object>
				{
					{ "SourceFileLoadingPeriod", sourceFileLoadingPeriod.TotalSeconds},
					{ "RecordCount", records.Length},
					{ "FileSize", sourceFileLength},
					{ "SourceFilePath", _sourceFilePath},
				});

			_sidecarManager = sidecarManager;
			_lineIndex = lineIndex;

			_analysisManager = new AnalysisManager(this, _coreExtension, _telemetryRecorder);

			var filterAliases = _coreExtension.GetFilterAliases(_context);
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
				_telemetryRecorder);

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
				records.Length,
				sourceFileLoadingPeriod,
				recordAndMetadataLoadingStopwatch.Elapsed);

			Debug.WriteLine($"{nameof(CoreEngine)} resources have been allocated.");
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

		~CoreEngine()
		{
			Log.Default.Write($"The core engine has been removed from memory.");
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

		public ImmutableArray<IRecord> Records => _scope.Records;

		public int Count => _scope.Records.Length;

		public bool HasBeenCleared => _hasBeenCleared;

		public ContextDictionary Context => _context;

		public string SourceFileRemarks
		{
			get { return _session.Remarks; }
			set { _session.Remarks = value; }
		}

		public string SourceFilePath => _sourceFilePath;

		public string SourceDirectory => Path.GetDirectoryName(_sourceFilePath);

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
			ImmutableArray<IRecord> selected = _selectionManager.GetSelected();

			ImmutableArray<IRecord> kept = new InMemoryRecordRepository(
					_scope.Records,
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
				kept.Length,
				_logFileMetrics.SourceFileLoadingPeriod,
				_logFileMetrics.RecordAndMetadataLoadDuration);

			// Not a user action, so no telemetry, as when the engine was created.
			_filterManager.ApplyInternal(FilterType.PlainText, FilterCriteria.None);

			Log.Default.Write(
				LogSeverityType.Information,
				"Records have been cleared.",
				new Dictionary<string, object>
				{
					{ "ClearOperation", clearOperation },
					{ "RecordsKept", kept.Length },
					{ "SelectedRecordsKept", _selectionManager.Selected.Count },
				});
		}

		private void OnResultsChanged(object sender, ResultsChangedEventArgs e)
		{
			// Before anyone else hears of the new results, so they read the selection that matches them (#1015).
			_selectionManager.KeepRecordsShownBy(e.Records);
			_navigationManager.UpdateDataSource(e.Records);
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
			using FileStream logSource = TryOpenSourceFile();

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
				ByteOffsetOfLine = lineNumber => GetByteOffset(logSource, lineNumber),
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

		private long GetByteOffset(Stream logSource, int lineNumber)
		{
			return logSource != null && lineNumber >= 1 && lineNumber <= _lineIndex.LineCount
				? _lineIndex.GetByteOffset(logSource, lineNumber)
				: -1;
		}

		public void GenerateReport(ReportType report, string destinationFolder)
		{
			if (report == ReportType.CommentSummary)
			{
				ImmutableArray<IRecord> filterResults = _filterManager.Results;
				new CommentSummaryReport(filterResults, _recordState).Generate(destinationFolder);
			}
		}
	}
}