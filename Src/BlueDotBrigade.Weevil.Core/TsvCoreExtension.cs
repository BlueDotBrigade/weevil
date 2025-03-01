namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using Analysis;
	using Data;
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
			_filterAliases = new Dictionary<string, string>
			{
				{ "#Trace", @"@Severity=Trace" },
				{ "#Debug", @"@Severity=Debug" },
				{ "#Information", @"@Severity=Information" },
				{ "#Warning", @"@Severity=Warning" },
				{ "#Error", @"@Severity=Error" },
				{ "#Critical", @"@Severity=Critical" },
				{ "#IPv4", @"(?<IPv4>((25[0-5]|(2[0-4]|1\\d|[1-9]|)\\d)(\\.(?!$)|$)){4})" },
				{ "#IPv6", @"(?<IPv6>(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4})" },
			};
			_metricCollectors = new List<IMetricCollector>
			{
				new SeverityMetrics()
			};
			_monikerActivators = new List<MonikerActivator>();
			_tableOfContents = new TableOfContents();
		}

		public string Name => GetType().AssemblyQualifiedName;

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