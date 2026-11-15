namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using BlueDotBrigade.Weevil.Data;

	public class TimeGapInsight : InsightBase
	{
		private static readonly TimeSpan DefaultThreshold = TimeSpan.FromSeconds(1);

		private readonly TimeSpan _threshold;

		public TimeGapInsight() : this(
			DefaultThreshold,
			"Time Gap",
			$"No gaps in logging were detected when using a threshold of {DefaultThreshold.ToHumanReadable()}.")
		{
			// nothing to do
		}

		public TimeGapInsight(TimeSpan threshold, string title, string defaultDetails) : base(
			title,
			"sec",
			"0",
			defaultDetails)
		{
			_threshold = threshold;
		}

		protected override void OnRefresh(RecordList records)
		{
			var analyzer = new TimeGapUiAnalyzer();
			analyzer.Analyze(records, state: null, _threshold, canUpdateMetadata: false);

			if (analyzer.Count > 0)
			{
				// The longest gap, in seconds as the unit says, and the record that ends it (#1226).
				this.MetricValue = analyzer.MaximumPeriodDetected.TotalSeconds.ToString("#,##0.0");
				this.IsAttentionRequired = true;
				this.RelatedLineNumbers = new[] { analyzer.MaximumPeriodLineNumber };

				var longestGapEndedAt = FindCreatedAt(records, analyzer.MaximumPeriodLineNumber);
				this.Details = $"A threshold of {_threshold.ToHumanReadable()}, resulted in {analyzer.Count} unexpected gaps in time. " +
				               $"The longest gap, {analyzer.MaximumPeriodDetected.ToHumanReadable()}, ended at {longestGapEndedAt:HH:mm:ss}.";
			}
		}

		private static DateTime FindCreatedAt(RecordList records, int lineNumber)
		{
			for (var position = 0; position < records.Count; position++)
			{
				if (records.GetLineNumber(position) == lineNumber)
				{
					return records.GetCreatedAt(position);
				}
			}

			return Record.CreationTimeUnknown;
		}
	}
}
