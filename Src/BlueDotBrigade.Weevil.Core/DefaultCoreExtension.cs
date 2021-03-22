namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using Analysis;
	using Data;
	using Filter.Expressions;
	using Navigation;

	internal class DefaultCoreExtension : ICoreExtension
	{
		private readonly ContextDictionary _context;
		private readonly IRecordParser _recordParser;
		private readonly IDictionary<string, string> _staticAliases;
		private readonly List<IRecordCounter> _recordCounters;
		private readonly IList<MonikerActivator> _monikerActivators;
		private readonly TableOfContents _tableOfContents;

		public DefaultCoreExtension()
		{
			var context = new ContextDictionary();

			_context = context;
			_recordParser = new DefaultRecordParser();
			_staticAliases = new Dictionary<string, string>();
			_recordCounters = new List<IRecordCounter>();
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

		public IList<IRecordCounter> GetRecordCounters(ContextDictionary context)
		{
			return _recordCounters;
		}

		public IList<MonikerActivator> GetMonikerActivators(ContextDictionary context)
		{
			return _monikerActivators;
		}

		public IDictionary<string, string> GetStaticAliases(ContextDictionary context)
		{
			return _staticAliases;
		}

		public TableOfContents BuildTableOfContents(StreamReader logFileReader)
		{
			return _tableOfContents;
		}
	}
}
