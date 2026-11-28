namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Runtime.ExceptionServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Analysis;
	using Data;
	using Diagnostics;

	[DebuggerDisplay("Results={Results.Count}, InclusiveFilter={_currentFilter.Criteria.Include}")]
	internal class FilterManager : IClonableInternally<FilterManager>, IFilter
	{
		public const int MaxFilterHistory = 32;

		#region Fields
		private readonly ICoreExtension _coreExtension;

		private readonly ContextDictionary _context;
		private readonly IFilterAliasExpander _filterAliasExpander;
		private readonly RecordScope _scope;
		private readonly ImmutableArray<IMetricCollector> _metricCollectors;
		private readonly RecordState _recordState;

		private readonly ITelemetryMetricRecorder _telemetryRecorder;

		// Whether the records keep their content (RAM) or read it back from the log (disk).
		private readonly IO.StorageMode _storageMode;

		// The file the records were read from: on disk, they read their content back through it. Null for the engine without records that Reload uses.
		private readonly IO.LogFileSource _source;

		private FilterStrategy _latestFilterStrategy;
		private RecordList _latestFilterResults;

		private readonly IRegionManager _regionManager;

		private readonly IBookmarkManager _bookmarkManager;

		private Filter _currentFilter;

		/// <summary>
		/// The elapsed times the current filter compared: those of the results shown before it (#1016).
		/// Applying the filter again compares them again, so @Elapsed keeps its results (#1224).
		/// </summary>
		private ElapsedTimes _comparedElapsedTimes = ElapsedTimes.None;

		// The histories belong to the log, so they live in the session (#1109).
		private readonly FilterHistory _includeHistory;
		private readonly FilterHistory _excludeHistory;

		/// <summary>
		/// One filter runs at a time. A newer filter waits here for the one it replaced to stop (#1248).
		/// </summary>
		private readonly SemaphoreSlim _runGate = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Guards the newest run and what a filter publishes, so a filter is either stopped or shown (#1248).
		/// </summary>
		private readonly object _runSync = new object();

		/// <summary>
		/// Stops the newest filter run, which Abort and the next filter cancel (#1248).
		/// </summary>
		private CancellationTokenSource _newestRun;

		/// <summary>
		/// The thread whose filter has its turn, or 0: a handler of that filter's events that applies a filter is refused, rather than waiting for itself (#1248).
		/// </summary>
		private int _turnThreadId;

		/// <summary>
		/// The metrics of <see cref="Results"/>. A stopped filter leaves the collectors part counted, so they are kept when a filter is published (#1248).
		/// </summary>
		private IDictionary<string, object> _metrics = new Dictionary<string, object>();

		private TimeSpan _filterExecutionTime;
		#endregion

		#region Object Lifetime
		public FilterManager(
			ICoreExtension coreExtension,
			ContextDictionary context,
			IFilterAliasExpander filterAliasExpander,
			RecordScope scope,
			ImmutableArray<IMetricCollector> metricCollectors,
			IRegionManager regionManager,
			IBookmarkManager bookmarkManager,
			RecordState recordState,
			FilterHistory includeHistory,
			FilterHistory excludeHistory,
			ITelemetryMetricRecorder telemetryRecorder = null,
			IO.StorageMode storageMode = IO.StorageMode.Ram,
			IO.LogFileSource source = null)
		{
			_coreExtension = coreExtension;
			_telemetryRecorder = telemetryRecorder ?? NullTelemetryMetricRecorder.Instance;
			_storageMode = storageMode;
			_source = source;
			_context = context;
			_filterAliasExpander = filterAliasExpander;
			_scope = scope;

			_regionManager = regionManager;
			_bookmarkManager = bookmarkManager;

			_metricCollectors = metricCollectors;
			_recordState = recordState ?? throw new ArgumentNullException(nameof(recordState));

			_latestFilterStrategy = FilterStrategy.KeepAllRecords;
			_latestFilterResults = scope.Records;

			_currentFilter = new Filter(FilterType.PlainText, FilterCriteria.None);

			_includeHistory = includeHistory ?? throw new ArgumentNullException(nameof(includeHistory));
			_excludeHistory = excludeHistory ?? throw new ArgumentNullException(nameof(excludeHistory));

			_filterExecutionTime = TimeSpan.Zero;
		}
		#endregion

		#region Properties
		public FilterStrategy FilterStrategy => _latestFilterStrategy;

		public RecordList Results => _latestFilterResults;

		public ElapsedTimes ElapsedTimes => _recordState.ElapsedTimes;

		/// <summary>
		/// The elapsed times the current filter compared, which applying it again compares too (#1224).
		/// </summary>
		internal ElapsedTimes ComparedElapsedTimes => _comparedElapsedTimes;

		public IReadOnlyList<string> IncludeHistory => _includeHistory;

		public IReadOnlyList<string> ExcludeHistory => _excludeHistory;

		public FilterType FilterType => _currentFilter.Type;

		public IFilterCriteria Criteria => _currentFilter.Criteria;

		public TimeSpan FilterExecutionTime => _filterExecutionTime;

		public IFilterAliasExpander AliasExpander => _filterAliasExpander;

		public event EventHandler<ResultsChangedEventArgs> ResultsChanged;
		public event EventHandler<HistoryChangedEventArgs> HistoryChanged;
		#endregion

		#region Private Methods
		FilterManager IClonableInternally<FilterManager>.CreateDeepCopy()
		{
			throw new NotImplementedException();
		}

		/// <remarks>
		/// Checks <paramref name="cancellationToken"/> after the loop too, so a Stop while the strategy was built is not lost (#1248).
		/// </remarks>
		private RecordList FilterAndCalculateMetrics(IFilterStrategy strategy, CancellationToken cancellationToken)
		{
			foreach (IMetricCollector collector in _metricCollectors)
			{
				collector.Reset();
			}

			// Records are created as they are read, so the filter marks the positions it keeps rather than keeping records (#1140).
			RecordList records = _scope.Records;
			var isKept = new bool[records.Count];
			var resultsCount = 0;

			// Set `MaxDegreeOfParallelism=1` to force the loop to be executed by only one thread.
			// ... This can simplify the debugging process.
			//
			// At most one worker per core, in RAM as on disk, because of a log on disk (#1269):
			// - The scenario: each worker holds a buffer of about 17 MB, the 16 MB chunk it reads. Without a cap, the thread pool adds
			//   workers while the others wait for the disk, for example with a cold cache or a log on a network share. A filter would then
			//   hold one buffer per thread, as many as the thread pool decides, rather than one per core.
			// - The cost: with a cold cache, a filter keeps fewer reads in flight. On 4 cores and the 10-million-record log,
			//   it was about 2% slower than without the cap (#1287).
			// - Kept by the owner's decision: a bounded working set matters more than those 2%.
			var workerCount = Environment.ProcessorCount;
			var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = workerCount };

			// On disk, each worker reads whole ranges of records that share a content chunk, so the log is read once (#1027).
			// In RAM, ranges of about the same size keep every core busy, however small the log.
			(int Start, int End)[] ranges = RecordRanges.Split(records, alignToChunks: _storageMode == IO.StorageMode.Disk, workerCount: workerCount);

			// The loop creates many more worker states than workers, so they share content buffers, which hold a chunk each on disk:
			// a filter allocates a buffer per worker, not per state (#1269).
			var buffers = new ConcurrentBag<ContentBuffer>();

			try
			{
				Parallel.ForEach(
								 // One range at a time, so no worker is left with several 16 MB chunks while the others wait.
								 Partitioner.Create((IEnumerable<(int Start, int End)>)ranges, EnumerablePartitionerOptions.NoBuffering),
								 parallelOptions,
						  // FMR Part1: initialize thread local storage
						  () => new FilterMapReduceState(buffers.TryTake(out ContentBuffer buffer) ? buffer : new ContentBuffer()),

						  // FMR Part2: define task to perform
						  (range, parallelLoopState, results) =>
						  {
							  for (var position = range.Start; position < range.End; position++)
							  {
								  if (cancellationToken.IsCancellationRequested)
								  {
									  parallelLoopState.Stop();
									  break;
								  }

								  IRecord record = records[position];

								  if (strategy.CanKeep(record, results.Content))
								  {
									  isKept[position] = true;
									  results.Count++;

									  foreach (IMetricCollector collector in _metricCollectors)
									  {
										  collector.Count(record);
									  }
								  }
							  }

							  return results;
						  },
						  // FMR Part3: merge the results
						  (results) =>
						  {
							  buffers.Add(results.Content);
							  Interlocked.Add(ref resultsCount, results.Count);
						  }
				);
			}
			catch (AggregateException exception) when (exception.Flatten().InnerExceptions.FirstOrDefault(inner => inner is IOException or UnauthorizedAccessException) is { } readError)
			{
				// A read of the log failed, in disk mode: throw what the read threw, as every other reader does (#1025).
				ExceptionDispatchInfo.Throw(readError);
			}

			if (cancellationToken.IsCancellationRequested)
			{
				throw Discard(strategy.FilterType, strategy.FilterCriteria);
			}

			// A filter that keeps every record keeps the records' list itself, so it keeps no array (#1140).
			if (resultsCount == records.Count)
			{
				return records;
			}

			var positions = new int[resultsCount];
			var kept = 0;

			for (var position = 0; position < isKept.Length; position++)
			{
				if (isKept[position])
				{
					positions[kept++] = position;
				}
			}

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Filter results have been kept. Records={records.Count}, Results={resultsCount}");

			return TableRecordList.Select(records, positions);
		}

		private static FilterAbortedException Discard(FilterType filterType, IFilterCriteria criteria)
		{
			Log.Default.Write(
				LogSeverityType.Warning,
				"Filter results have been discarded - the filter was stopped, or a newer filter replaced it.",
				new Dictionary<string, object>
				{
					{"FilterType", filterType },
					{"FilterCriteria", criteria },
				});

			return new FilterAbortedException();
		}

		/// <summary>
		/// Makes a run the newest, and stops the one before it: only the newest filter's results are shown (#1248).
		/// </summary>
		private CancellationTokenSource StartRun()
		{
			var run = new CancellationTokenSource();

			lock (_runSync)
			{
				_newestRun?.Cancel();
				_newestRun = run;
			}

			return run;
		}

		private void EndRun(CancellationTokenSource run)
		{
			lock (_runSync)
			{
				if (_newestRun == run)
				{
					_newestRun = null;
				}
			}

			run.Dispose();
		}

		private IDictionary<string, object> CollectMetrics()
		{
			var metrics = new Dictionary<string, object>();

			foreach (IMetricCollector collector in _metricCollectors)
			{
				foreach (KeyValuePair<string, object> result in collector.GetResults())
				{
					metrics.Add(result.Key, result.Value);
				}
			}

			return metrics;
		}

		private void UpdateFilterHistory(FilterHistory history, string newValue)
		{
			if (!string.IsNullOrWhiteSpace(newValue))
			{
				// Raised after the session's lock is released, as the comment and pin store raises its own events.
				foreach (HistoryChangedEventArgs change in history.Add(newValue))
				{
					HistoryChanged?.Invoke(history, change);
				}
			}
		}

		protected virtual void RaiseFilterChanged(ResultsChangedEventArgs e)
		{
			EventHandler<ResultsChangedEventArgs> threadSafeHandler = ResultsChanged;

			if (threadSafeHandler != null)
			{
				try
				{
					threadSafeHandler(this, e);
				}
				catch (Exception exception)
				{
					Log.Default.Write(LogSeverityType.Error, exception);
				}
			}
		}

		protected virtual void RaiseHistoryChanged(HistoryChangedEventArgs e)
		{
			EventHandler<HistoryChangedEventArgs> threadSafeHandler = HistoryChanged;

			if (threadSafeHandler != null)
			{
				try
				{
					threadSafeHandler(this, e);
				}
				catch (Exception exception)
				{
					Log.Default.Write(LogSeverityType.Error, exception);
				}
			}
		}
#endregion

#region Event Handlers
#endregion


		/// <summary>
		/// Re-applies the most recent filter to the in-memory records.
		/// </summary>
		/// <returns></returns>
		public IFilter ReApply()
		{
			Log.Default.Write(
				LogSeverityType.Information,
				"Re-applying the most recent filter to the in-memory records...");

			// Re-applying is not a user-initiated filter action, so it is not recorded as telemetry.
			// It compares the elapsed times the filter compared before, not those of its own results (#1224).
			// Read together, as a filter publishes them (#1248).
			Filter filter;
			ElapsedTimes comparedElapsedTimes;

			lock (_runSync)
			{
				filter = _currentFilter;
				comparedElapsedTimes = _comparedElapsedTimes;
			}

			return ApplyInternal(filter.Type, filter.Criteria, comparedElapsedTimes);
		}

		/// <remarks>
		/// Applying a filter, even the same one again, measures elapsed times over the records shown now, because new options or a Clear
		/// change which records are visible (#1231). Only <see cref="ReApply"/> (F5) and Reload keep the elapsed times the filter compared (#1224).
		/// </remarks>
		public IFilter Apply(FilterType filterType, IFilterCriteria criteria)
		{
			_telemetryRecorder.Increment(TelemetryMetrics.FilterApplied);
			return ApplyInternal(filterType, criteria);
		}

		/// <param name="previousElapsedTimes">
		/// The elapsed times that @Elapsed compares: by default, those of the results shown before this filter (#1016).
		/// The filter keeps them until the next one, and they keep those results.
		/// </param>
		internal IFilter ApplyInternal(FilterType filterType, IFilterCriteria criteria, ElapsedTimes previousElapsedTimes = null)
		{
			ArgumentNullException.ThrowIfNull(criteria);

			if (Volatile.Read(ref _turnThreadId) == Environment.CurrentManagedThreadId)
			{
				throw new InvalidOperationException("A filter cannot be applied while handling the events of the filter being applied, because it would wait for that filter to end.");
			}

			var stopwatch = Stopwatch.StartNew();
			CancellationTokenSource run = StartRun();

			try
			{
				Run(filterType, criteria, previousElapsedTimes, run, stopwatch);
			}
			finally
			{
				EndRun(run);
			}

			return this;
		}

		private static (long WorkingSetBytes, long PeakWorkingSetBytes)? ReadProcessMemory()
		{
			try
			{
				using var process = Process.GetCurrentProcess();
				return (process.WorkingSet64, process.PeakWorkingSet64);
			}
			catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or NotSupportedException)
			{
				// Process memory counters are optional diagnostics.
				return null;
			}
		}

		private void Run(FilterType filterType, IFilterCriteria criteria, ElapsedTimes previousElapsedTimes, CancellationTokenSource run, Stopwatch stopwatch)
		{
			try
			{
				Log.Default.Write(
					LogSeverityType.Information,
					$"Filtering records...",
					new Dictionary<string, object>
					{
						{"FilterType", filterType},
						{"FilterCriteria", criteria},
					});

				// Read once this run is the newest, which no older filter publishes after (#1248): these are the times of the results shown.
				ElapsedTimes comparedElapsedTimes = previousElapsedTimes ?? _recordState.ElapsedTimes;

				// Built before this filter's turn, so its expressions compile while the filter it replaces stops.
				var strategy =
					new FilterStrategy(_coreExtension, _context, _filterAliasExpander, filterType, criteria, _regionManager, _bookmarkManager, _recordState, comparedElapsedTimes);

				try
				{
					// Stop, or a newer filter, ends the wait for the filter this one replaced (#1248).
					_runGate.Wait(run.Token);
				}
				catch (OperationCanceledException)
				{
					throw Discard(filterType, criteria);
				}

				Volatile.Write(ref _turnThreadId, Environment.CurrentManagedThreadId);

				try
				{
					var isOnDisk = _storageMode == IO.StorageMode.Disk;
					var bytesReadBefore = _source?.BytesRead ?? 0;
					(long WorkingSetBytes, long PeakWorkingSetBytes)? memoryBefore = isOnDisk ? ReadProcessMemory() : null;
					var gen2Before = isOnDisk ? GC.CollectionCount(2) : 0;
					var allocatedBefore = isOnDisk ? GC.GetTotalAllocatedBytes(precise: true) : 0;
					var scanStarted = isOnDisk ? Stopwatch.GetTimestamp() : 0;

					RecordList results = FilterAndCalculateMetrics(strategy, run.Token);

					TimeSpan scanElapsed = isOnDisk ? Stopwatch.GetElapsedTime(scanStarted) : TimeSpan.Zero;
					var allocatedBytes = isOnDisk ? GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore : 0;
					var gen2Collections = isOnDisk ? GC.CollectionCount(2) - gen2Before : 0;
					(long WorkingSetBytes, long PeakWorkingSetBytes)? memoryAfter = isOnDisk ? ReadProcessMemory() : null;
					IDictionary<string, object> metrics = CollectMetrics();
					var elapsedTimes = ElapsedTimes.Calculate(results);

					// A filter is either stopped or shown, never both: Stop, and a newer filter's start, take the same lock (#1016, #1248).
					bool isStopped;

					lock (_runSync)
					{
						isStopped = run.IsCancellationRequested;

						if (!isStopped)
						{
							_latestFilterStrategy = strategy;
							_latestFilterResults = results;
							_metrics = metrics;
							_currentFilter = new Filter(filterType, criteria);
							_comparedElapsedTimes = comparedElapsedTimes;
							_recordState.ElapsedTimes = elapsedTimes;
						}
					}

					if (isStopped)
					{
						throw Discard(filterType, criteria);
					}

					UpdateFilterHistory(_includeHistory, criteria.Include);
					UpdateFilterHistory(_excludeHistory, criteria.Exclude);

					// Raised during this filter's turn, so a newer filter's results are always heard last (#1248).
					RaiseFilterChanged(new ResultsChangedEventArgs(results));

					_filterExecutionTime = stopwatch.Elapsed;

					var completed = new Dictionary<string, object>
					{
						{"FilterType", filterType},
						{"FilterCriteria", criteria},
						{"TimeElapsed", _filterExecutionTime},
						{"TotalRecords", _scope.Records.Count},
						{"VisibleRecords", results.Count},
					};

					if (isOnDisk)
					{
						// What the filter read from a log on disk, including what other readers read meanwhile, such as the rows on screen (#1027).
						completed.Add("BytesRead", (_source?.BytesRead ?? 0) - bytesReadBefore);
						completed.Add("ProcessorCount", Environment.ProcessorCount);
						completed.Add("FilterScanElapsed", scanElapsed);
						completed.Add("ProcessManagedAllocatedBytes", allocatedBytes);
						completed.Add("ProcessGen2Collections", gen2Collections);

						if (memoryBefore is { } before && memoryAfter is { } after)
						{
							completed.Add("ProcessWorkingSetBeforeBytes", before.WorkingSetBytes);
							completed.Add("ProcessWorkingSetAfterBytes", after.WorkingSetBytes);
							completed.Add("ProcessPeakWorkingSetBeforeBytes", before.PeakWorkingSetBytes);
							completed.Add("ProcessPeakWorkingSetAfterBytes", after.PeakWorkingSetBytes);
						}
					}

					Log.Default.Write(LogSeverityType.Information, $"Filtering complete.", completed);
				}
				finally
				{
					Volatile.Write(ref _turnThreadId, 0);
					_runGate.Release();
				}
			}
			catch (Exception exception) when (exception is not IO.LogFileChangedException and not FilterAbortedException)
			{
				// A log that changed or is gone is expected in disk mode, and its source has already logged a warning (#1025, #1295).
				Log.Default.Write(
					LogSeverityType.Error,
					exception,
					"Filtering operation encountered an unexpected error.",
					new Dictionary<string, object>
					{
						{ "FilterType", filterType},
						{ "FilterCriteria", criteria },
					});
				throw;
			}
		}

		public IDictionary<string, object> GetMetrics()
		{
			return new Dictionary<string, object>(_metrics);
		}

		public void Abort()
		{
			lock (_runSync)
			{
				_newestRun?.Cancel();
			}
		}
	}
}