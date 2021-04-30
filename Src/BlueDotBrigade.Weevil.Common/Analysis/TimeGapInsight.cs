namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	public class TimeGapInsight : InsightBase
	{
		private readonly bool _uiThreadOnly;
		private static readonly TimeSpan DefaultThreshold = TimeSpan.FromSeconds(1);

		private readonly TimeSpan _threshold;

		public TimeGapInsight(bool uiThreadOnly) : this(
			uiThreadOnly, 
			DefaultThreshold,
			"Time Gap",
			$"Using a threshold of {DefaultThreshold.ToHumanReadable()}, there are no gaps in time.")
		{
			// nothing to do
		}

		public TimeGapInsight(bool uiThreadOnly, TimeSpan threshold, string title, string details) : base(
			title,
			"sec",
			"0",
			details)
		{
			_uiThreadOnly = uiThreadOnly;
			_threshold = threshold;
		}

		protected override void OnRefresh(ImmutableArray<IRecord> records)
		{
			var analyzer = new TimeGapAnalyzer(_uiThreadOnly);
			analyzer.Analyze(records, _threshold, false);

			if (analyzer.Count > 0)
			{
				this.MetricValue = analyzer.MaximumPeriodDetected.TotalSeconds.ToString("#,##0.0");
				this.IsAttentionRequired = true;
				this.Details = $"A threshold of {_threshold.ToHumanReadable()}, resulted in {analyzer.Count} unexpected gaps in time. " +
				               $"The longest gap occurred at {analyzer.FirstOccurrenceAt.ToString("HH:mm:ss")}.";
			}
		}
	}
}
