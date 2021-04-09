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

		IList<IRecordAnalyzer> GetAnalyzers();
		ImmutableArray<IInsight> GetInsights(ContextDictionary context);
		IList<IRecordCounter> GetRecordCounters(ContextDictionary context);
		IList<MonikerActivator> GetMonikerActivators(ContextDictionary context);
		IDictionary<string, string> GetStaticAliases(ContextDictionary context);
		TableOfContents BuildTableOfContents(StreamReader logFileReader);
	}
}