namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	internal class UiResponsivenessInsight : InsightBase
	{
		public UiResponsivenessInsight() : base(
			"Unresponsive UI",
			"sec",
			"0",
			"Indicates how often the user interface took longer than 1s to respond to a user request.")
		{
			// nothing to do
		}

		protected override void OnRefresh(ImmutableArray<IRecord> records)
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();
			analyzer.Analyze(records, TimeSpan.FromSeconds(1));

			if (analyzer.UnresponsiveUiCount > 0)
			{
				this.MetricValue = analyzer.MaximumPeriodDetected.TotalSeconds.ToString("###.0");
				this.IsAttentionRequired = true;
				this.Details = $"The user interface may have been unresponsive {analyzer.UnresponsiveUiCount} time(s). " +
				               $"The worst case scenario occurred at {analyzer.FirstOccurrenceAt.ToString("HH:mm:ss")}.";
			}
		}
	}
}
