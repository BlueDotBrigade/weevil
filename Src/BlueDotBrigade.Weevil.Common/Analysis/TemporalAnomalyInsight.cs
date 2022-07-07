namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	public class TemporalAnomalyInsight : InsightBase
	{
		public TemporalAnomalyInsight() : base(
			"Temporal Anomalies",
			"Σ",
			"0",
			"The log file timestamps are in chronological order.")
		{
		}

		protected override void OnRefresh(ImmutableArray<IRecord> records)
		{
			var metrics = new TemporalAnomalyMetrics();

			foreach (IRecord record in records)
			{
				metrics.Count(record);
			}

			if (metrics.Counter > 0)
			{
				this.MetricValue = metrics.Counter.ToString("#,##0");
				this.IsAttentionRequired = true;
				this.Details = $"The log file timestamps were not in chronological order {metrics.Counter} time(s). " +
				               $"The first anomaly occurred at {metrics.FirstOccurredAt.CreatedAt.ToString("HH:mm:ss")}.";
			}
		}
	}
}