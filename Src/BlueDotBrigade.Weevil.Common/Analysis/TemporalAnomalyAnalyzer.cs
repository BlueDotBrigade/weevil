namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	public class TemporalAnomalyAnalyzer : IRecordAnalyzer
	{
		private const string CommentLabel = "TemporalAnomaly";

		public string Key => AnalysisType.TemporalAnomaly.ToString();

		public string DisplayName => "Temporal Anomaly";

		public int Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var counter = 0;

			var metric = new TemporalAnomalyMetrics(TimeSpan.Zero);

			foreach (IRecord record in records)
			{
				metric.Count(record);

				if (canUpdateMetadata)
				{
					if (metric.Counter == counter)
					{
						record.Metadata.IsFlagged = false;
					}
					else
					{
						counter++;

						record.Metadata.IsFlagged = true;
						record.Metadata.UpdateUserComment($"{CommentLabel}");
					}
				}
			}

			return metric.Counter;
		}
	}
}