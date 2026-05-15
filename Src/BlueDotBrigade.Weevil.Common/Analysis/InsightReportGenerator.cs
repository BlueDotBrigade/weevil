namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	public class InsightReportGenerator
	{
		public InsightReport Generate(Version weevilVersion, IEngine engine, ImmutableArray<IInsight> insights, DateTime from, DateTime to)
		{
			if (weevilVersion is null)
			{
				throw new ArgumentNullException(nameof(weevilVersion));
			}

			if (engine is null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			var fileName = Path.GetFileName(engine.SourceFilePath);
			var context = engine.Context.Count == 0 ? "Not specified" : engine.Context.ToString();

			var reportItems = insights
				.Where(insight => insight is not null)
				.Select(ToReportItem)
				.ToImmutableArray();

			return new InsightReport
			{
				Title = $"Weevil Insight: {fileName}",
				SourceFileName = fileName,
				Context = context,
				WeevilVersion = weevilVersion,
				From = from,
				To = to,
				ProblemAreas = reportItems.Where(item => item.IsAttentionRequired).ToImmutableArray(),
				MoreInformation = reportItems.Where(item => !item.IsAttentionRequired).ToImmutableArray()
			};
		}

		private static InsightReportItem ToReportItem(IInsight insight)
		{
			if (insight is null)
			{
				throw new ArgumentNullException(nameof(insight));
			}

			return new InsightReportItem
			{
				Title = insight.Title,
				MetricValue = insight.MetricValue,
				MetricUnit = insight.MetricUnit,
				Details = insight.Details,
				IsAttentionRequired = insight.IsAttentionRequired,
				RelatedRecords = insight.RelatedRecords
					.Where(Record.IsGenuine)
					.Select(record => new InsightRelatedRecord
					{
						LineNumber = record.LineNumber,
						CreatedAt = record.HasCreationTime ? record.CreatedAt : null,
						Preview = record.Content
					})
					.ToImmutableArray()
			};
		}
	}
}
