namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	public class InsightReportGenerator
	{
		/// <summary>
		/// Builds the insight report as a model, which a renderer then turns into Markdown or another format (#837).
		/// </summary>
		/// <remarks>
		/// Reads no record text: the insights keep line numbers (#1139). Call it after <see cref="IAnalyze.GetInsights"/>,
		/// so that the filter's metrics describe the same log, if it was reloaded.
		/// </remarks>
		/// <param name="weevilVersion">The version of Weevil that builds the report.</param>
		/// <param name="engine">The open log.</param>
		/// <param name="insights">The insights to report, in the order they are shown.</param>
		/// <param name="from">The start of the period analyzed. <see langword="null"/> or <see cref="Record.CreationTimeUnknown"/> when it is not known.</param>
		/// <param name="to">The end of the period analyzed. <see langword="null"/> or <see cref="Record.CreationTimeUnknown"/> when it is not known.</param>
		public InsightReport Create(Version weevilVersion, IEngine engine, ImmutableArray<IInsight> insights, DateTime? from, DateTime? to)
		{
			ArgumentNullException.ThrowIfNull(weevilVersion);
			ArgumentNullException.ThrowIfNull(engine);

			var fileName = Path.GetFileName(engine.SourceFilePath);

			return new InsightReport
			{
				SourceFileName = fileName,
				Context = new Dictionary<string, string>(engine.Context),
				WeevilVersion = weevilVersion.ToString(),
				GeneratedAt = DateTimeOffset.Now,
				From = KnownOrNull(from),
				To = KnownOrNull(to),
				RecordCount = engine.Metrics.RecordCount,
				FileSize = engine.Metrics.FileSize,
				Metrics = engine.Filter
					.GetMetrics()
					.Select(metric => new InsightReportMetric { Name = metric.Key, Value = metric.Value })
					.ToImmutableArray(),
				Insights = insights
					.Select(insight => new InsightReportItem
					{
						Title = insight.Title,
						MetricValue = insight.MetricValue,
						MetricUnit = insight.MetricUnit,
						Details = insight.Details,
						IsAttentionRequired = insight.IsAttentionRequired,
						RelatedLineNumbers = insight.RelatedLineNumbers?.ToImmutableArray() ?? ImmutableArray<int>.Empty,
					})
					.ToImmutableArray(),
			};
		}

		/// <summary>
		/// The time, or <see langword="null"/> when it is <see cref="Record.CreationTimeUnknown"/>, as an estimated range returns for a log without timestamps.
		/// </summary>
		private static DateTime? KnownOrNull(DateTime? time)
		{
			return time == Record.CreationTimeUnknown ? null : time;
		}

		/// <summary>
		/// Returns the insight report as Markdown.
		/// </summary>
		/// <remarks>
		/// Through <see cref="Create"/>, so it also reads the engine's metrics and the filter's.
		/// </remarks>
		[Obsolete("Build the report with Create, then render it with MarkdownInsightReportRenderer (#837).")]
		public string Generate(Version weevilVersion, IEngine engine, ImmutableArray<IInsight> insights, DateTime from, DateTime to)
		{
			return new MarkdownInsightReportRenderer().Render(Create(weevilVersion, engine, insights, from, to));
		}
	}
}
