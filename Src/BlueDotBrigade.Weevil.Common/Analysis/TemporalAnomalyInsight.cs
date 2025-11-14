namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
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
			// nothing to do
		}

		protected override void OnRefresh(ImmutableArray<IRecord> records)
		{
			var metrics = new TemporalAnomalyMetrics(TimeSpan.Zero);

			foreach (IRecord record in records)
			{
				metrics.Count(record);
			}

			if (metrics.Counter > 0)
			{
				var threshold = metrics.Threshold.Equals(TimeSpan.Zero)
					? "any discrepancy."
					: metrics.Threshold.ToHumanReadable();

				this.MetricValue = metrics.Counter.ToString("#,##0");
				this.IsAttentionRequired = false; // When using NLog, log entries are often out of order.
				this.Details = $"The log file timestamps were not in chronological order {metrics.Counter} time(s). " +
				               $"The biggest anomaly occurred at {metrics.BiggestAnomalyAt.CreatedAt.ToString("HH:mm:ss")}, " +
				               $"and was {metrics.BiggestAnomaly.ToHumanReadable()}. Using threshold: {threshold}";

				// Store the record where the biggest anomaly occurred
				if (metrics.BiggestAnomalyAt != null && !Record.IsDummyOrNull(metrics.BiggestAnomalyAt))
				{
					this.RelatedRecords = ImmutableArray.Create(metrics.BiggestAnomalyAt);
				}
			}
		}
	}
}