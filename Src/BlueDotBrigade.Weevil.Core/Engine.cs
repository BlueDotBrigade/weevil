namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Runtime.CompilerServices;
	using Analysis;
	using Data;
	using Filter;
	using Navigation;
	using Reports;

	[DebuggerDisplay("InstanceId={_coreEngine.InstanceId}, Records={_coreEngine.Count}, Context={_coreEngine.Context.ToString()}")]
	public class Engine : IEngine
	{
		public static readonly IEngine Surrogate;

		private CoreEngine _coreEngine;

		internal Engine(CoreEngine coreEngine)
		{
			_coreEngine = coreEngine;
			SubscribeTo(_coreEngine);
		}

		/// <inheritdoc />
		public event EventHandler<ResultsChangedEventArgs> FilterResultsChanged;

		/// <inheritdoc />
		public event EventHandler<HistoryChangedEventArgs> FilterHistoryChanged;

		[SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Method required to build complex instance.")]
		static Engine()
		{
			// Constants for Unix-like system paths
			const string LocalShareDirectory = ".local";
			const string ShareDirectory = "share";
			const string WeevilDirectory = "Weevil";
			
			// Cross-platform support: LocalAppData is Windows-specific
			// On Linux/Mac, fall back to user's home directory
			var localAppData = Environment.GetEnvironmentVariable("LocalAppData");
			if (string.IsNullOrEmpty(localAppData))
			{
				// On Unix-like systems, use XDG_DATA_HOME or ~/.local/share
				localAppData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
				if (string.IsNullOrEmpty(localAppData))
				{
					var home = Environment.GetEnvironmentVariable("HOME");
					if (!string.IsNullOrEmpty(home))
					{
						localAppData = Path.Combine(home, LocalShareDirectory, ShareDirectory);
					}
					else
					{
						// Final fallback: use temp directory
						localAppData = Path.GetTempPath();
					}
				}
			}
			
			var directoryPath = Path.Combine(localAppData, WeevilDirectory);

			Directory.CreateDirectory(directoryPath);

			var filePath = Path.Combine(directoryPath, "EmptyFile.log");

			if (!File.Exists(filePath))
			{
				File.WriteAllText(filePath, string.Empty);
			}

			Surrogate = UsingPath(filePath).Open();
		}


		public static bool IsRealInstance(IEngine instance)
		{
			var isSurrogate = instance?.Equals(Surrogate) ?? false;

			return !isSurrogate;
		}

		public IRecord this[int index] => _coreEngine[index];

		public RecordList Records => _coreEngine.Records;

		public LogFileMetrics Metrics => _coreEngine.Metrics;

		public IFilter Filter => _coreEngine.Filter;

		public INavigate Navigate => _coreEngine.Navigate;

		public ISelect Selector => _coreEngine.Selector;

		public IAnalyze Analyzer => _coreEngine.Analyzer;

		public IRegionManager Regions => _coreEngine.Regions;

		public IBookmarkManager Bookmarks => _coreEngine.Bookmarks;

		public IAnnotations Annotations => _coreEngine.Session.Annotations;

		public IRecordState RecordState => _coreEngine.RecordState;

		public int Count => _coreEngine.Count;

		public ContextDictionary Context => _coreEngine.Context;

		public string SourceFileRemarks
		{
			get
			{
				return _coreEngine.SourceFileRemarks;
			}
			set
			{
				_coreEngine.SourceFileRemarks = value;
			}
		}

		public string SourceFilePath => Engine.IsRealInstance(this)
			? _coreEngine.SourceFilePath
			: string.Empty;

		public string SourceDirectory => Engine.IsRealInstance(this)
			? _coreEngine.SourceDirectory
			: string.Empty;

		public bool HasBeenCleared => _coreEngine.HasBeenCleared;

		public void Save()
		{
			_coreEngine.Save();
		}

		public void Save(bool deleteSidecarBackup)
		{
			_coreEngine.Save(deleteSidecarBackup);
		}

		public static IEngineBuilder UsingPath(string sourceFilePath)
		{
			return new EngineBuilder(sourceFilePath);
		}

		public static IEngineBuilder UsingPath(string sourceFilePath, int startAtLineNumber)
		{
			return new EngineBuilder(sourceFilePath, startAtLineNumber);
		}

		public void Clear(ClearOperation clearOperation)
		{
			// Saved first: once cleared, a record's comment and pin are no longer in memory to be saved (#1004).
			if (Engine.IsRealInstance(this))
			{
				_coreEngine.Save();
			}

			// Clear resets the filter without telling subscribers, as IEngine documents; the engine itself still hears of it.
			UnsubscribeFrom(_coreEngine);

			try
			{
				_coreEngine.Clear(clearOperation);
			}
			finally
			{
				SubscribeTo(_coreEngine);
			}
		}

		public void Reload()
		{
			(CoreEngine.CoreEngineBuilder builder, SelectionManager.Snapshot selection, FilterType filterType, IFilterCriteria filterCriteria) = Release();

			Replace(builder.Build());

			// Before the filter, which then narrows the selection as any filter does (#1062).
			_coreEngine.Selector.Restore(selection);
			_coreEngine.Filter.Apply(filterType, filterCriteria);
		}

		/// <summary>
		/// Keeps what Reload carries over, then lets go of the records, so the engine holds none of them while it reads the log again (#1126).
		/// Meanwhile, and if reading the log fails, this engine shows the same log without records.
		/// </summary>
		/// <remarks>
		/// A method of its own, because a Debug build keeps a method's temporaries, such as the old filter, until it returns.
		/// </remarks>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private (CoreEngine.CoreEngineBuilder Builder, SelectionManager.Snapshot Selection, FilterType FilterType, IFilterCriteria FilterCriteria) Release()
		{
			// The session and the flags carry across Reload (#1013).
			CoreEngine.CoreEngineBuilder builder = CoreEngine
				.FromPath(_coreEngine.SourceFilePath)
				.UsingStateOf(_coreEngine);

			var kept = (builder, _coreEngine.Selector.TakeSnapshot(), _coreEngine.Filter.FilterType, _coreEngine.Filter.Criteria);

			// Not the Surrogate's engine, which every engine shares: after a failed read, this engine still saves to and reloads its own log.
			Replace(_coreEngine.WithoutRecords());

			return kept;
		}

		/// <summary>
		/// Moves the subscriptions to the new engine's filter, so callers subscribe to this engine only once (#1011).
		/// </summary>
		private void Replace(CoreEngine coreEngine)
		{
			UnsubscribeFrom(_coreEngine);
			_coreEngine = coreEngine;
			SubscribeTo(_coreEngine);
		}

		private void SubscribeTo(CoreEngine coreEngine)
		{
			coreEngine.Filter.ResultsChanged += OnFilterResultsChanged;
			coreEngine.Filter.HistoryChanged += OnFilterHistoryChanged;
		}

		private void UnsubscribeFrom(CoreEngine coreEngine)
		{
			coreEngine.Filter.ResultsChanged -= OnFilterResultsChanged;
			coreEngine.Filter.HistoryChanged -= OnFilterHistoryChanged;
		}

		private void OnFilterResultsChanged(object sender, ResultsChangedEventArgs e)
		{
			FilterResultsChanged?.Invoke(sender, e);
		}

		private void OnFilterHistoryChanged(object sender, HistoryChangedEventArgs e)
		{
			// The sender stays the history list, which callers use to tell the include history from the exclude history.
			FilterHistoryChanged?.Invoke(sender, e);
		}

		public void GenerateReport(ReportType report, string destinationFolder)
		{
			_coreEngine.GenerateReport(report, destinationFolder);
		}
	}
}