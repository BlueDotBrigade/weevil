namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
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

		protected override void OnRefresh(RecordList records)
		{
			var metrics = new TemporalAnomalyMetrics(TimeSpan.Zero);

			// From the columns, so refreshing the insight after each open creates no record (#1150).
			for (var position = 0; position < records.Count; position++)
			{
				metrics.Count(records.GetLineNumber(position), records.GetCreatedAt(position));
			}

			if (metrics.Counter > 0)
			{
				var threshold = metrics.Threshold.Equals(TimeSpan.Zero)
					? "any discrepancy."
					: metrics.Threshold.ToHumanReadable();

				this.MetricValue = metrics.Counter.ToString("#,##0");
				this.IsAttentionRequired = false; // When using NLog, log entries are often out of order.
				this.Details = $"The log file timestamps were not in chronological order {metrics.Counter} time(s). " +
				               $"The biggest anomaly occurred at {metrics.BiggestAnomalyAt.ToString("HH:mm:ss")}, " +
				               $"and was {metrics.BiggestAnomaly.ToHumanReadable()}. Using threshold: {threshold}";

				// Store the line where the biggest anomaly occurred
				if (metrics.BiggestAnomalyOnLine != Record.Dummy.LineNumber)
				{
					this.RelatedLineNumbers = new[] { metrics.BiggestAnomalyOnLine };
				}
			}
		}
	}
}