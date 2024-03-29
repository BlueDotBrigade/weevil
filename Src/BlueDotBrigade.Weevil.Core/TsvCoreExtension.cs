﻿namespace BlueDotBrigade.Weevil
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
		private readonly IDictionary<string, string> _filterAliases;
		private readonly List<IMetricCollector> _metricCollectors;
		private readonly IList<MonikerActivator> _monikerActivators;
		private readonly TableOfContents _tableOfContents;

		public TsvCoreExtension()
		{
			var context = new ContextDictionary();

			_context = context;
			_recordParser = new TsvRecordParser();
			_filterAliases = new Dictionary<string, string>();
			_metricCollectors = new List<IMetricCollector>
			{
				new SeverityMetrics()
			};
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

		public IList<IRecordAnalyzer> GetAnalyzers(ContextDictionary context, ITableOfContents tableOfContents)
		{
			return new List<IRecordAnalyzer>();
		}

		public IList<IRecordAnalyzer> GetAnalyzers(ICoreEngine coreEngine, ContextDictionary context, ITableOfContents tableOfContents)
		{
			return new List<IRecordAnalyzer>();
		}

		public ImmutableArray<IInsight> GetInsights(ContextDictionary context, ITableOfContents tableOfContents, ImmutableArray<IInsight> defaultInsights)
		{
			return defaultInsights;
		}

		public IList<IMetricCollector> GetRecordCounters(ContextDictionary context)
		{
			return _metricCollectors;
		}

		public IList<MonikerActivator> GetMonikerActivators(ContextDictionary context)
		{
			return _monikerActivators;
		}

		public IDictionary<string, string> GetFilterAliases(ContextDictionary context)
		{
			return _filterAliases;
		}

		public TableOfContents BuildTableOfContents(StreamReader logFileReader)
		{
			return _tableOfContents;
		}
	}
}
