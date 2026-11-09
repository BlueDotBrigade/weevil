namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Runtime.ExceptionServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Analysis;
	using Data;
	using Diagnostics;

	[DebuggerDisplay("Results={Results.Length}, InclusiveFilter={_currentFilter.Criteria.Include}")]
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

		private FilterStrategy _latestFilterStrategy;
		private RecordList _latestFilterResults;

		private IRegionManager _regionManager;

		private IBookmarkManager _bookmarkManager;

		private Filter _currentFilter;

		// The histories belong to the log, so they live in the session (#1109).
		private readonly FilterHistory _includeHistory;
		private readonly FilterHistory _excludeHistory;

		private bool _abortFilterOperation;

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
			ITelemetryMetricRecorder telemetryRecorder = null)
		{
			_coreExtension = coreExtension;
			_telemetryRecorder = telemetryRecorder ?? NullTelemetryMetricRecorder.Instance;
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

		private RecordList FilterAndCalculateMetrics(IFilterStrategy strategy)
		{
			_abortFilterOperation = false;

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
			var parallelOptions = new ParallelOptions();

			try
			{
				Parallel.ForEach(
								 records,
								 parallelOptions,
						  // FMR Part1: initialize thread local storage
						  () => { return new FilterMapReduceState(); },

						  // FMR Part2: define task to perform
						  (record, parallelLoopState, index, results) =>
						  {
							  if (_abortFilterOperation)
							  {
								  parallelLoopState.Break();
							  }

							  if (strategy.CanKeep(record, results.Content))
							  {
								  isKept[index] = true;
								  results.Count++;

								  foreach (IMetricCollector collector in _metricCollectors)
								  {
									  collector.Count(record);
								  }
							  }

							  return results;
						  },
						  // FMR Part3: merge the results
						  (results) =>
						  {
							  Interlocked.Add(ref resultsCount, results.Count);
						  }
				);
			}
			catch (AggregateException exception) when (exception.Flatten().InnerExceptions.OfType<IOException>().FirstOrDefault() is { } readError)
			{
				// A read of the log failed, in disk mode: throw what the read threw, as every other reader does (#1025).
				ExceptionDispatchInfo.Throw(readError);
			}

			if (_abortFilterOperation)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					$"Filter results have been discarded - the user has aborted the filter operation.",
					new Dictionary<string, object>
					{
						{"FilterType", strategy.FilterType },
						{"FilterCriteria", strategy.FilterCriteria },
					});

				throw new FilterAbortedException();
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
			return ApplyInternal(_currentFilter.Type, _currentFilter.Criteria);
		}

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
			if (criteria is null)
			{
				throw new ArgumentNullException(nameof(criteria));
			}

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

				_latestFilterStrategy =
					new FilterStrategy(_coreExtension, _context, _filterAliasExpander, filterType, criteria, _regionManager, _bookmarkManager, _recordState, previousElapsedTimes ?? _recordState.ElapsedTimes);

				_filterExecutionTime = TimeSpan.Zero;
				var exectionTimeStopwatch = Stopwatch.StartNew();

				_latestFilterResults = FilterAndCalculateMetrics(_latestFilterStrategy);

				_currentFilter = new Filter(filterType, criteria);

				UpdateFilterHistory(_includeHistory, criteria.Include);
				UpdateFilterHistory(_excludeHistory, criteria.Exclude);

				// An aborted filter never gets here, so the results still shown keep their elapsed times (#1016).
				_recordState.ElapsedTimes = ElapsedTimes.Calculate(_latestFilterResults);

				exectionTimeStopwatch.Stop();
				_filterExecutionTime = exectionTimeStopwatch.Elapsed;

				Log.Default.Write(
					LogSeverityType.Information,
					$"Filtering complete.",
					new Dictionary<string, object>
					{
						{"FilterType", filterType},
						{"FilterCriteria", criteria},
						{"TimeElapsed", _filterExecutionTime},
						{"TotalRecords", _scope.Records.Length},
						{"VisibleRecords", _latestFilterResults.Length},
					});
			}
			catch (Exception exception) when (exception is not IO.LogFileChangedException)
			{
				// A log that changed is expected in disk mode, and its source has already logged a warning (#1025).
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

			RaiseFilterChanged(new ResultsChangedEventArgs(_latestFilterResults));

			return this;
		}

		public IDictionary<string, object> GetMetrics()
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

		public void Abort()
		{
			_abortFilterOperation = true;
		}
	}
}