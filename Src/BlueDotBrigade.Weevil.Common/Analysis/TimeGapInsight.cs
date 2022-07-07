namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
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
			"Σ",
			defaultDetails)
		{
			_threshold = threshold;
		}

		protected override void OnRefresh(ImmutableArray<IRecord> records)
		{
			var analyzer = new TimeGapUiAnalyzer();
			analyzer.Analyze(records, _threshold, false);

			if (analyzer.Count > 0)
			{
				this.MetricValue = analyzer.Count.ToString("#,##0.0");
				this.IsAttentionRequired = true;
				this.Details = $"A threshold of {_threshold.ToHumanReadable()}, resulted in {analyzer.Count} unexpected gaps in time. " +
				               $"The longest gap occurred at {analyzer.FirstOccurrenceAt.ToString("HH:mm:ss")}.";
			}
		}
	}
}
