namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using Timeline;

	internal class AnalysisManager : IAnalyze
	{
		private const int MetricKeyMaxLength = 128;

		private readonly ICoreExtension _coreExtension;
		private readonly CoreEngine _coreEngine;
		private readonly ITelemetryMetricRecorder _telemetryRecorder;

		internal AnalysisManager(CoreEngine coreEngine, ICoreExtension coreExtension, ITelemetryMetricRecorder telemetryRecorder = null)
		{
			_coreEngine = coreEngine;
			_coreExtension = coreExtension;
			_telemetryRecorder = telemetryRecorder ?? NullTelemetryMetricRecorder.Instance;
		}

		/// <remarks>
		/// One change to the session, as Remove Comments is, so the grid refreshes once (decision 4 of the #990 plan).
		/// The pinned lines are found in parallel, outside the store's lock. Only the records in memory are unpinned, as before.
		/// </remarks>
		public void UnpinAll()
		{
			AnnotationStore annotations = _coreEngine.Session.Annotations;

			annotations.SetPinned(FindLines(_coreEngine.Records, lineNumber => annotations.IsPinned(lineNumber)), false);
		}

		/// <remarks>
		/// Every line, including cleared ones, or their flags would come back on Reload (#1013).
		/// </remarks>
		public void RemoveAllFlags()
		{
			_coreEngine.RecordState.Flags.Clear();
		}

		/// <remarks>
		/// Every line is unflagged, including cleared ones, as <see cref="RemoveAllFlags"/> does. A cleared line is not flagged,
		/// or its flag would come back on Reload (#1013). One change to the flags, which raise no events.
		/// </remarks>
		public int FlagOnly(IEnumerable<int> lineNumbers)
		{
			RecordList records = _coreEngine.Records;

			// A binary search for each line: there are few, and the records are in line order.
			int[] inMemory = lineNumbers
				.Distinct()
				.Where(lineNumber => records.TryIndexOfLineNumber(lineNumber, out _))
				.ToArray();

			_coreEngine.RecordState.Flags.FlagOnly(inMemory);

			return inMemory.Length;
		}

		public void RemoveComments(bool clearAll)
		{
			RecordList records = clearAll ? _coreEngine.Records : _coreEngine.Selector.GetSelected();
			AnnotationStore annotations = _coreEngine.Session.Annotations;

			// Any text, so a comment of white space alone is removed too.
			annotations.RemoveComments(FindLines(records, lineNumber => annotations.GetComment(lineNumber).Length > 0));
		}

		public IList<IRecordAnalyzer> GetAnalyzers(ComponentType componentType)
		{
			var analyzers = new List<IRecordAnalyzer>();

			if ((componentType & ComponentType.Core) == ComponentType.Core)
			{
				var filterStrategy = _coreEngine.Filter.FilterStrategy;
				var aliasExpander = _coreEngine.Filter.AliasExpander;

				analyzers.AddRange(new List<IRecordAnalyzer>()
				{
					new TimeGapAnalyzer(),
					new TimeGapUiAnalyzer(),
					new OutOfOrderTimestampsAnalyzer(),
					new DetectDataAnalyzer(filterStrategy, aliasExpander),
					new FirstOccurrenceAnalyzer(filterStrategy, aliasExpander),
					new LastOccurrenceAnalyzer(filterStrategy, aliasExpander),
					new StableValueRunsAnalyzer(filterStrategy, aliasExpander),
					new StateTransitionsAnalyzer(filterStrategy, aliasExpander),
					new DetectRisingEdgeAnalyzer(filterStrategy, aliasExpander),
					new DetectFallingEdgeAnalyzer(filterStrategy, aliasExpander),
					new ThresholdCrossingsAnalyzer(filterStrategy, aliasExpander),
					new MatchingRecordRunsAnalyzer(filterStrategy, aliasExpander),
					new StatisticalAnalyzer(filterStrategy, aliasExpander),
				});
			}

			if ((componentType & ComponentType.Extension) == ComponentType.Extension)
			{
				analyzers.AddRange(_coreExtension.GetAnalyzers(
					_coreEngine.Context,
					_coreEngine.Navigate.TableOfContents));

				analyzers.AddRange(_coreExtension.GetAnalyzers(
					_coreEngine,
					_coreEngine.Context,
					_coreEngine.Navigate.TableOfContents));
			}

			return analyzers;
		}

		public ImmutableArray<IInsight> GetInsights()
		{
			Log.Default.Write(LogSeverityType.Debug, "Insight is being gathered from the recordset...");

			var defaultInsights = new List<IInsight>
			{
				new CriticalErrorsInsight(),
				new TimeGapInsight(),
				new TemporalAnomalyInsight(),
			};

			var stopwatch = Stopwatch.StartNew();

			ImmutableArray<IInsight> insights = _coreExtension.GetInsights(
				_coreEngine.Context,
				_coreEngine.Navigate.TableOfContents,
				defaultInsights.ToImmutableArray());

			foreach (IInsight insight in insights)
			{
				insight.Refresh(_coreEngine.Records);
			}

			stopwatch.Stop();

			var attentionRequiredCount = insights.Count(x => x.IsAttentionRequired);

			Log.Default.Write(LogSeverityType.Information, $"Insight has been gathered. Records={_coreEngine.Records.Length:###,###,###,###}, Insights={insights.Count()}, AttentionRequired={attentionRequiredCount}, ExecutionTime={stopwatch.Elapsed.ToHumanReadable()}");

			return ImmutableArray.Create(insights.ToArray());
		}

		public void Analyze(AnalysisType analysisType)
		{
			var analyzerKey = analysisType.ToString();
			Analyze(analyzerKey, new UserDialogNotRequired());
		}

		public Results Analyze(AnalysisType analysisType, IUserDialog userDialog)
		{
			var analyzerKey = analysisType.ToString();
			return Analyze(analyzerKey, userDialog);
		}

		public Results Analyze(string analyzerKey, IUserDialog userDialog)
		{
			RecordList records = _coreEngine.Selector.HasSelectionPeriod
				? _coreEngine.Selector.GetSelected()
				: _coreEngine.Filter.Results;

			IRecordAnalyzer analyzer = GetAnalyzers(ComponentType.All).First(x => x.Key == analyzerKey);

			Results results = analyzer.Analyze(
				records,
				_coreEngine.RecordState,
				_coreEngine.SourceDirectory,
				userDialog,
				true);

			// Records which analyzer the user ran. Works for plugin analyzers too: their key flows
			// through unchanged and becomes a new metric_key row (no schema/code change required).
			_telemetryRecorder.Increment(BuildAnalysisMetricKey(analyzerKey));

			return results;
		}

		/// <summary>
		/// Composes the per-analyzer metric key, e.g. <c>Analysis.Run.DetectData</c>, capped to the
		/// <c>metric_key</c> column length.
		/// </summary>
		internal static string BuildAnalysisMetricKey(string analyzerKey)
		{
			var key = $"{TelemetryMetrics.AnalysisRun}.{analyzerKey}";
			return key.Length <= MetricKeyMaxLength ? key : key.Substring(0, MetricKeyMaxLength);
		}

		/// <summary>
		/// Returns the line numbers of the records that match, searched in parallel.
		/// </summary>
		private static int[] FindLines(RecordList records, Func<int, bool> isMatch)
		{
			return records
				.AsParallel()
				.Where(record => isMatch(record.LineNumber))
				.Select(record => record.LineNumber)
				.ToArray();
		}
	}
}
