namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Reflection;
	using System.Threading;
	using Analysis;
	using Configuration.Sidecar;
	using Data;
	using Diagnostics;
	using Filter;
	using Navigation;
	using Reports;
	using Reports.Timeline;

	[DebuggerDisplay("InstanceId={_instanceId}, Records={Count}, Results={Filter.Results.Length}")]
	internal partial class CoreEngine : ICoreEngine
	{
		#region Fields
		private readonly ICoreExtension _coreExtension;

		private readonly string _sourceFilePath;
		private readonly ContextDictionary _context;

		private readonly LogFileMetrics _logFileMetrics;

		private readonly ImmutableArray<IRecord> _allRecords;

		private readonly FilterManager _filterManager;
		private readonly SidecarManager _sidecarManager;
		private readonly SelectionManager _selectionManager;
		private readonly NavigationManager _navigationManager;
		private readonly IAnalyze _analysisManager;

		private readonly int _originalRecordCount;

		private readonly bool _wasLoadedFromDisk;

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
		/// Facilitates the creation of a new <see cref="CoreEngine"/> object using a Fluent API and the current instance.
		/// </summary>
		/// <param name="clearOperation">Indicates what records will be omitted from the current object, when a new instance is created..</param>
		internal CoreEngineBuilder FromInstance(ClearRecordsOperation clearOperation)
		{
			return new CoreEngineBuilder(this, clearOperation);
		}

		/// <summary>
		/// Creates an instance of <see cref="CoreEngine"/> which can only be instantiated through a Fluent API managed by <see cref="CoreEngineBuilder"/>.
		/// </summary>
		private CoreEngine(
			string sourceFilePath,
			long sourceFileLength,
			ICoreExtension coreExtension,
			ContextDictionary context,
			SidecarManager sidecarManager,
			ImmutableArray<IRecord> records,
			TableOfContents tableOfContents)
		{
			_instanceId = Interlocked.Increment(ref _instancesCreated);

			if (string.IsNullOrWhiteSpace(sourceFilePath))
			{
				throw new ArgumentException("A valid file path was expected.", nameof(sourceFilePath));
			}

			LogCoreEngineVersion();

			_coreExtension = coreExtension;
			_context = context;

			_sourceFilePath = sourceFilePath;
			_allRecords = records;
			_wasLoadedFromDisk = true;

			Log.Default.Write(
				LogSeverityType.Debug,
				"File loading...");

			var recordLoadingStopwatch = Stopwatch.StartNew();
			var recordAndMetadataLoadingStopwatch = Stopwatch.StartNew();

			recordLoadingStopwatch.Stop();

			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

			Log.Default.Write(
				LogSeverityType.Information,
				"File loading has retrieved records from disk.",
				new Dictionary<string, object>
				{
					{ "RecordLoadDuration", recordLoadingStopwatch.Elapsed.TotalSeconds},
					{ "RecordCount", _allRecords.Length},
					{ "FileSize", sourceFileLength},
					{ "SourceFilePath", _sourceFilePath},
				});

			_sidecarManager = sidecarManager;

			_originalRecordCount = _allRecords.Length;

			new ElapsedTimeAnalyzer(_allRecords).Analyze();

			_analysisManager = new AnalysisManager(this, _coreExtension);

			IStaticAliasExpander staticAliasExpander = _coreExtension.GetStaticAliasExpander(_context);

			_filterManager = new FilterManager(
				_coreExtension,
				_context,
				staticAliasExpander,
				_allRecords,
				GetRecordCounters());

			_filterManager.Apply(FilterType.PlainText, FilterCriteria.None);
			_filterManager.ResultsChanged += OnResultsChanged;

			_navigationManager = new NavigationManager(_sourceFilePath, _coreExtension, _allRecords, tableOfContents);

			_selectionManager = new SelectionManager(
				_allRecords,
				_filterManager.Results,
				_navigationManager, // TODO: adding navigation manager is a code smell
				new Action(() => { _filterManager.ReApply(); }));

			recordAndMetadataLoadingStopwatch.Stop();

			_logFileMetrics = new LogFileMetrics(
				sourceFileLength,
				_allRecords.Length,
				recordLoadingStopwatch.Elapsed,
				recordAndMetadataLoadingStopwatch.Elapsed);

			var filterDuration =
				_logFileMetrics.RecordAndMetadataLoadDuration.TotalSeconds -
				_logFileMetrics.RecordLoadDuration.TotalSeconds;

			Log.Default.Write(
				LogSeverityType.Information,
				"File loading complete.",
				new Dictionary<string, object>
				{
					{ "RecordAndMetadataLoadDuration", _logFileMetrics.RecordAndMetadataLoadDuration.TotalSeconds },
					{ "RecordLoadDuration", _logFileMetrics.RecordLoadDuration.TotalSeconds },
					{ "FilterDuration", filterDuration },
					{ "RecordCount", _logFileMetrics.RecordCount },
					{ "FileSize", _logFileMetrics.FileSize },
					{ "SourceFilePath", _sourceFilePath},
				});

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
		public IRecord this[int index] => _allRecords[index];

		public LogFileMetrics Metrics => _logFileMetrics;

		public FilterManager Filter => _filterManager;

		public NavigationManager Navigator => _navigationManager;

		public SelectionManager Selector => _selectionManager;

		public IAnalyze Analyzer => _analysisManager;

		public ImmutableArray<IRecord> Records => _allRecords;

		public int Count => _allRecords.Length;

		public bool IsSameAsDisk => _wasLoadedFromDisk;

		public ContextDictionary Context => _context;

		public string SourceFilePath => _sourceFilePath;

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

		INavigate ICoreEngine.Navigator => this.Navigator;

		ISelect ICoreEngine.Selector => this.Selector;

		#endregion

		#region Private Methods

		private ImmutableArray<IRecordCounter> GetRecordCounters()
		{
			IList<IRecordCounter> recordCounters = _coreExtension.GetRecordCounters(_context);
			recordCounters.Add(new SeverityMetrics());
			return recordCounters.ToImmutableArray();
		}

		private void OnResultsChanged(object sender, ResultsChangedEventArgs e)
		{
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

			var sidecarData = new SidecarData
			{
				Records = _allRecords,
				Context = _context,
				FilterTraits = _filterManager,
				TableOfContents = _navigationManager.TableOfContents,
			};

			_sidecarManager.Save(sidecarData, deleteBackup);
		}

		public void GenerateReport(ReportType report, string destinationFolder)
		{
			if (report == ReportType.CommentSummary)
			{
				ImmutableArray<IRecord> filterResults = _filterManager.Results;
				new CommentSummaryReport(filterResults).Generate(destinationFolder);
			}
		}
	}
}