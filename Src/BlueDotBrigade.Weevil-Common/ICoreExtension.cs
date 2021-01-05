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

	/// <summary>
	/// Represents an extension of the <see cref="ICoreEngine"/> which can be used to support proprietary log formats.
	/// </summary>
	public interface ICoreExtension
	{
		string Name { get; }

		IRecordParser GetRecordParser();

		ContextDictionary DetermineContext(ImmutableArray<IRecord> allRecords);

		IList<IRecordAnalyzer> GetAnalyzers();
		IRecordAnalyzer GetAnalyzer(string analyzerKey, ICoreEngine coreEngine, ImmutableArray<IRecord> allRecords);
		IList<IRecordCounter> GetRecordAnalyzers(ContextDictionary context);
		IList<MonikerActivator> GetMonikerActivators(ContextDictionary context);
		IStaticAliasExpander GetStaticAliasExpander(ContextDictionary context);
		TableOfContents BuildTableOfContents(StreamReader logFileReader);
	}
}