namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using Analysis;
	using Data;
	using Diagnostics;

	[DebuggerDisplay("Results={Results.Length}, InclusiveFilter={_currentFilter.Criteria.Include}")]
	internal class FilterManager : IClonableInternally<FilterManager>, IFilter
	{
		public const int MaxFilterHistory = 16;

		#region Fields
		private readonly ICoreExtension _coreExtension;

		private readonly ContextDictionary _context;
		private readonly IFilterAliasExpander _filterAliasExpander;
		private readonly ImmutableArray<IRecord> _allRecords;
		private readonly ImmutableArray<IMetricCollector> _metricCollectors;

		private FilterStrategy _latestFilterStrategy;
		private ImmutableArray<IRecord> _latestFilterResults;

		private Filter _currentFilter;

		private readonly IList<string> _includeHistory;
		private readonly IList<string> _excludeHistory;

		private bool _abortFilterOperation;

		private TimeSpan _filterExecutionTime;
		#endregion

		#region Object Lifetime
		public FilterManager(
			ICoreExtension coreExtension,
			ContextDictionary context,
			IFilterAliasExpander filterAliasExpander,
			ImmutableArray<IRecord> allRecords,
			ImmutableArray<IMetricCollector> metricCollectors)
		{
			_coreExtension = coreExtension;
			_context = context;
			_filterAliasExpander = filterAliasExpander;
			_allRecords = allRecords;

			_metricCollectors = metricCollectors;

			_latestFilterStrategy = FilterStrategy.KeepAllRecords;
			_latestFilterResults = allRecords;

			_currentFilter = new Filter(FilterType.PlainText, FilterCriteria.None);

			_includeHistory = new List<string>();
			_excludeHistory = new List<string>();

			_filterExecutionTime = TimeSpan.Zero;
		}
		#endregion

		#region Properties
		public FilterStrategy FilterStrategy => _latestFilterStrategy;

		public ImmutableArray<IRecord> Results => _latestFilterResults;

		public IList<string> IncludeHistory => _includeHistory;

		public IList<string> ExcludeHistory => _excludeHistory;

		public FilterType FilterType => _currentFilter.Type;

		public IFilterCriteria Criteria => _currentFilter.Criteria;

		public TimeSpan FilterExecutionTime => _filterExecutionTime;

		public event EventHandler<ResultsChangedEventArgs> ResultsChanged;
		public event EventHandler<HistoryChangedEventArgs> HistoryChanged;
		#endregion

		#region Private Methods
		FilterManager IClonableInternally<FilterManager>.CreateDeepCopy()
		{
			throw new NotImplementedException();
		}

		private ImmutableArray<IRecord> FilterAndCalculateMetrics(IFilterStrategy strategy)
		{
			_abortFilterOperation = false;

			foreach (IMetricCollector analyzer in _metricCollectors)
			{
				analyzer.Reset();
			}

			TimeSpan ElapsedTimeUnknown = TimeSpan.MinValue;

			var resultsCache = new IRecord[_allRecords.Length];
			var resultsCount = 0;

			// Set `MaxDegreeOfParallelism=1` to force the loop to be executed by only one thread.
			// ... This can simplify the debugging process.
			var parallelOptions = new ParallelOptions();

			Parallel.ForEach(
							 _allRecords,
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

						  if (strategy.CanKeep(record))
						  {
							  resultsCache[index] = record;
							  results.Count++;

							  foreach (IMetricCollector collector in _metricCollectors)
							  {
								  collector.Count(record);
							  }
						  }
						  else
						  {
							  record.Metadata.ElapsedTime = ElapsedTimeUnknown;
							  // this record does not meet the search criteria
							  // ... it will not be included in the results
							  resultsCache[index] = null;
						  }

						  return results;
					  },
					  // FMR Part3: merge the results
					  (results) =>
					  {
						  Interlocked.Add(ref resultsCount, results.Count);
					  }
			);

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

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Filter results are being compressed... Before={resultsCache.Length}");

			ImmutableArray<IRecord> compressedResults = ImmutableArray.Create(resultsCache).Compact(resultsCount);

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Filter results have been compressed. Before={resultsCache.Length}, After={compressedResults.Length}");

			return compressedResults;
		}

		private void UpdateFilterHistory(IList<string> history, string newValue)
		{
			if (!string.IsNullOrWhiteSpace(newValue))
			{
				if (history.Contains(newValue))
				{
					var insertAt = 0;
					var removeAt = history.IndexOf(newValue);
					var oldValue = history[removeAt];

					// Move item
					history.RemoveAt(removeAt);
					history.Insert(insertAt, newValue);

					HistoryChanged?.Invoke(
						history,
						new HistoryChangedEventArgs(HistoryChangeType.Moved, removeAt, oldValue));
				}
				else
				{
					if (history.Count >= MaxFilterHistory)
					{
						var removeAt = history.Count - 1;
						var oldValue = history[removeAt];
						history.RemoveAt(removeAt);

						HistoryChanged?.Invoke(
						history,
						new HistoryChangedEventArgs(HistoryChangeType.Removed, removeAt, oldValue));
					}

					var insertAt = 0;
					history.Insert(insertAt, newValue);
					HistoryChanged?.Invoke(
						history,
						new HistoryChangedEventArgs(HistoryChangeType.Added, insertAt, newValue));
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

			return Apply(_currentFilter.Type, _currentFilter.Criteria);
		}

		public IFilter Apply(FilterType filterType, IFilterCriteria criteria)
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
					new FilterStrategy(_coreExtension, _context, _filterAliasExpander, filterType, criteria);

				_filterExecutionTime = TimeSpan.Zero;
				var exectionTimeStopwatch = Stopwatch.StartNew();

				_latestFilterResults = FilterAndCalculateMetrics(_latestFilterStrategy);

				_currentFilter = new Filter(filterType, criteria);

				UpdateFilterHistory(_includeHistory, criteria.Include);
				UpdateFilterHistory(_excludeHistory, criteria.Exclude);

				new ElapsedTimeAnalyzer(_latestFilterResults).Analyze();

				exectionTimeStopwatch.Stop();
				_filterExecutionTime = exectionTimeStopwatch.Elapsed;

				GC.Collect();

				Log.Default.Write(
					LogSeverityType.Information,
					$"Filtering complete.",
					new Dictionary<string, object>
					{
						{"FilterType", filterType},
						{"FilterCriteria", criteria},
						{"TimeElapsed", _filterExecutionTime},
						{"TotalRecords", _allRecords.Length},
						{"VisibleRecords", _latestFilterResults.Length},
					});
			}
			catch (Exception exception)
			{
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