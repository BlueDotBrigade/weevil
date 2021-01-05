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

		public IList<IRecordCollectionAnalyzer> GetAnalyzers(ComponentType componentType)
		{
			var analyzers = new List<IRecordCollectionAnalyzer>();

			if ((componentType & ComponentType.Core) == ComponentType.Core)
			{
				analyzers.AddRange(new List<IRecordCollectionAnalyzer>()
				{
					new UiResponsivenessAnalyzer(),
					new DetectDataAnalyzer(_coreEngine.Filter.FilterStrategy),
					new DataTransitionAnalyzer(_coreEngine.Filter.FilterStrategy),
					new DetectFallingEdgeAnalyzer(_coreEngine.Filter.FilterStrategy),
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
			var analysisKey = analysisType.ToString();
			return GetAnalyzers(ComponentType.Core).First(x => x.Key == analysisKey);
		}
	}
}
