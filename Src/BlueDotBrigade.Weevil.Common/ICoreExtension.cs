namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using Analysis;
	using Data;
	using Filter.Expressions;
	using Navigation;

	/// <summary>
	/// Represents an extension of the <see cref="ICoreEngine"/> which can be used to support proprietary log formats.
	/// </summary>
	public interface ICoreExtension
	{
		string Name { get; }

		IRecordParser GetRecordParser();

		ContextDictionary DetermineContext(ImmutableArray<IRecord> allRecords);

		IList<IRecordAnalyzer> GetAnalyzers(ContextDictionary context, ITableOfContents tableOfContents);
		ImmutableArray<IInsight> GetInsights(ContextDictionary context, ITableOfContents tableOfContents);
		IList<IMetricCollector> GetRecordCounters(ContextDictionary context);
		IList<MonikerActivator> GetMonikerActivators(ContextDictionary context);
		IDictionary<string, string> GetFilterAliases(ContextDictionary context);
		TableOfContents BuildTableOfContents(StreamReader logFileReader);

		IList<string> GetGraphPatternOptions();
	}
}