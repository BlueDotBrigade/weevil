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

		/// <summary>
		/// Request insight that is appropriate for the current plugin.
		/// </summary>
		/// <param name="context">
		/// Includes metadata that describes how the log file was created (e.g. the application version).
		/// </param>
		/// <param name="tableOfContents">
		/// A plugin specific data structure that identifies key sections of a log file.
		/// </param>
		/// <param name="defaultInsights">
		/// General insight is provided, and the underlying plugin will determine what will be included in the final list.
		/// </param>
		/// <returns>
		/// Returns a list of <see cref="IInsight"/> that will be used to analyze the log file for anomalies.
		/// </returns>
		ImmutableArray<IInsight> GetInsights(ContextDictionary context, ITableOfContents tableOfContents, ImmutableArray<IInsight> defaultInsights);
		IList<IMetricCollector> GetRecordCounters(ContextDictionary context);
		IList<MonikerActivator> GetMonikerActivators(ContextDictionary context);
		IDictionary<string, string> GetFilterAliases(ContextDictionary context);
		TableOfContents BuildTableOfContents(StreamReader logFileReader);
	}
}