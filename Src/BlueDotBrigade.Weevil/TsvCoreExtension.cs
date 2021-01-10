namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using Analysis;
	using Data;
	using Filter;
	using Filter.Expressions;
	using Navigation;

	internal class TsvCoreExtension : ICoreExtension
	{
		private readonly ContextDictionary _context;
		private readonly IRecordParser _recordParser;
		private readonly IStaticAliasExpander _staticAliasExpander;
		private readonly List<IRecordCounter> _recordAnalyzers;
		private readonly IRecordAnalyzer _recordCollectionAnalyzer;
		private readonly IList<MonikerActivator> _monikerActivators;
		private readonly TableOfContents _tableOfContents;

		public TsvCoreExtension()
		{
			var context = new ContextDictionary();

			_context = context;
			_recordParser = new TsvRecordParser();
			_staticAliasExpander = new DefaultAliasExpander();
			_recordAnalyzers = new List<IRecordCounter>();
			_recordCollectionAnalyzer = new DefaultCollectionAnalyzer();
			_monikerActivators = new List<MonikerActivator>();
			_tableOfContents = new TableOfContents();
		}

		public string Name => GetType().Assembly.FullName;

		public IRecordParser GetRecordParser()
		{
			return _recordParser;
		}

		public ContextDictionary DetermineContext(ImmutableArray<IRecord> allRecords)
		{
			return _context;
		}

		public IList<IRecordAnalyzer> GetAnalyzers()
		{
			return new List<IRecordAnalyzer>();
		}

		public IRecordAnalyzer GetAnalyzer(string analyzerKey, ICoreEngine coreEngine, ImmutableArray<IRecord> allRecords)
		{
			return _recordCollectionAnalyzer;
		}

		public IRecordAnalyzer GetAnalyzer(AnalysisType analysisType, ICoreEngine coreEngine,
			ImmutableArray<IRecord> allRecords)
		{
			return _recordCollectionAnalyzer;
		}

		public IList<IRecordCounter> GetRecordCounters(ContextDictionary context)
		{
			return _recordAnalyzers;
		}

		public IList<MonikerActivator> GetMonikerActivators(ContextDictionary context)
		{
			return _monikerActivators;
		}

		public IStaticAliasExpander GetStaticAliasExpander(ContextDictionary context)
		{
			return _staticAliasExpander;
		}

		public TableOfContents BuildTableOfContents(StreamReader logFileReader)
		{
			return _tableOfContents;
		}
	}
}
