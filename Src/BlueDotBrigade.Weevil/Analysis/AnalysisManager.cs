namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;
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

		public IRecordCollectionAnalyzer GetAnalyzer(AnalysisType analysisType)
		{
			IRecordCollectionAnalyzer analyzer = null;

			switch (analysisType)
			{
				case AnalysisType.UiResponsiveness:
					analyzer = new UiResponsivenessAnalyzer(_coreEngine.Filter.Results);
					break;

				case AnalysisType.ExtractRegExKvp:
					analyzer = new DetectDataAnalyzer(_coreEngine.Filter.FilterStrategy, _coreEngine.Filter.Results);
					break;

				case AnalysisType.DataTransition:
					analyzer = new DataTransitionAnalyzer(_coreEngine.Filter.FilterStrategy, _coreEngine.Filter.Results);
					break;

				case AnalysisType.DataTransitionFallingEdge:
					analyzer = new DetectFallingEdgeAnalyzer(_coreEngine.Filter.FilterStrategy, _coreEngine.Filter.Results);
					break;

				//default:
				//	analyzer = _coreExtension.GetAnalyzer(analysisType, _coreEngine, _coreEngine.AllRecords);
				//	break;
			}

			return analyzer;
		}
	}
}
