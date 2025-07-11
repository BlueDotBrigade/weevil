﻿namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;
	using Timeline;

	internal class AnalysisManager : IAnalyze
	{
		private readonly ICoreExtension _coreExtension;
		private readonly CoreEngine _coreEngine;

		internal AnalysisManager(CoreEngine coreEngine, ICoreExtension coreExtension)
		{
			_coreEngine = coreEngine;
			_coreExtension = coreExtension;
		}

		public void UnpinAll()
		{
			Parallel.For(0, _coreEngine.Records.Length - 1, i =>
			{
				_coreEngine.Records[i].Metadata.IsPinned = false;
			});
		}

		public void RemoveAllFlags()
		{
			Parallel.For(0, _coreEngine.Records.Length - 1, i =>
			{
				_coreEngine.Records[i].Metadata.IsFlagged = false;
			});
		}

		public void RemoveComments(bool clearAll)
		{
			if (clearAll)
			{
				Parallel.For(0, _coreEngine.Records.Length, i =>
				{
					_coreEngine.Records[i].Metadata.Comment = string.Empty;
				});
			}
			else
			{
				ImmutableArray<Data.IRecord> selectedRecords = _coreEngine.Selector.GetSelected();
				Parallel.For(0, selectedRecords.Length, i =>
				{
					selectedRecords[i].Metadata.Comment = string.Empty;
				});
			}
		}

		public IList<IRecordAnalyzer> GetAnalyzers(ComponentType componentType)
		{
			var analyzers = new List<IRecordAnalyzer>();

			if ((componentType & ComponentType.Core) == ComponentType.Core)
			{
				analyzers.AddRange(new List<IRecordAnalyzer>()
				{
					new TimeGapAnalyzer(),
					new TimeGapUiAnalyzer(),
					new TemporalAnomalyAnalyzer(),
                                       new DetectDataAnalyzer(_coreEngine.Filter.FilterStrategy),
                                       new DetectFirstAnalyzer(_coreEngine.Filter.FilterStrategy),
                                       new DataTransitionAnalyzer(_coreEngine.Filter.FilterStrategy),
					new DetectRisingEdgeAnalyzer(_coreEngine.Filter.FilterStrategy),
					new DetectFallingEdgeAnalyzer(_coreEngine.Filter.FilterStrategy),
					new DetectRepeatingRecordsAnalyzer(_coreEngine.Filter.FilterStrategy),
					new StatisticalAnalyzer(_coreEngine.Filter.FilterStrategy),
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
			ImmutableArray<IRecord> records = _coreEngine.Selector.HasSelectionPeriod
				? _coreEngine.Selector.GetSelected()
				: _coreEngine.Filter.Results;

			IRecordAnalyzer analyzer = GetAnalyzers(ComponentType.All).First(x => x.Key == analyzerKey);

			Results results = analyzer.Analyze(
				records,
				_coreEngine.SourceDirectory,
				userDialog,
				true);

			return results;
		}
	}
}