namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Threading.Tasks;
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
			Parallel.For(0, _coreEngine.AllRecords.Length - 1, i =>
			{
				_coreEngine.AllRecords[i].Metadata.IsPinned = false;
			});
		}

		public void RemoveAllFlags()
		{
			Parallel.For(0, _coreEngine.AllRecords.Length - 1, i =>
			{
				_coreEngine.AllRecords[i].Metadata.IsFlagged = false;
			});
		}

		public void RemoveComments(bool clearAll)
		{
			if (clearAll)
			{
				Parallel.For(0, _coreEngine.AllRecords.Length, i =>
				{
					_coreEngine.AllRecords[i].Metadata.Comment = string.Empty;
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

		public IList<IRecordCollectionAnalyzer> GetAnalyzers(ComponentType componentType)
		{
			var analyzers = new List<IRecordCollectionAnalyzer>();

			if ((componentType & ComponentType.Core) == ComponentType.Core)
			{
				analyzers.AddRange(new List<IRecordCollectionAnalyzer>()
				{
					new UiResponsivenessAnalyzer(_coreEngine.Filter.Results),
					new DetectDataAnalyzer(_coreEngine.Filter.FilterStrategy, _coreEngine.Filter.Results),
					new DataTransitionAnalyzer(_coreEngine.Filter.FilterStrategy, _coreEngine.Filter.Results),
					new DetectFallingEdgeAnalyzer(_coreEngine.Filter.FilterStrategy, _coreEngine.Filter.Results),
				});
			}

			if ((componentType & ComponentType.Extension) == ComponentType.Extension)
			{
				analyzers.AddRange(_coreExtension.GetAnalyzers());
			}

			return analyzers;
		}

		public IRecordCollectionAnalyzer GetAnalyzer(string analyzerKey)
		{
			return GetAnalyzers(ComponentType.All).First(x => x.Key == analyzerKey);
		}

		public IRecordCollectionAnalyzer GetAnalyzer(AnalysisType analysisType)
		{
			IRecordCollectionAnalyzer analyzer = null;

			switch (analysisType)
			{
				case AnalysisType.DetectUnresponsiveUi:
					analyzer = new UiResponsivenessAnalyzer(_coreEngine.Filter.Results);
					break;

				case AnalysisType.DetectData:
					analyzer = new DetectDataAnalyzer(_coreEngine.Filter.FilterStrategy, _coreEngine.Filter.Results);
					break;

				case AnalysisType.DetectDataTransition:
					analyzer = new DataTransitionAnalyzer(_coreEngine.Filter.FilterStrategy, _coreEngine.Filter.Results);
					break;

				case AnalysisType.DetectFallingEdges:
					analyzer = new DetectFallingEdgeAnalyzer(_coreEngine.Filter.FilterStrategy, _coreEngine.Filter.Results);
					break;
			}

			return analyzer;
		}
	}
}
