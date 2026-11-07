namespace BlueDotBrigade.Weevil.Analysis
{
	using BlueDotBrigade.Weevil.Data;

	public class CriticalErrorsInsight : InsightBase
	{
		public CriticalErrorsInsight() : base(
			"Critical Errors",
			"Σ",
			"0",
			"No critical or fatal errors have been detected.")
		{
			// nothing to do
		}

		protected override void OnRefresh(RecordList records)
		{
			var analyzer = new SeverityMetrics();

			// From the columns, so refreshing the insight after each open creates no record (#1150).
			for (var position = 0; position < records.Count; position++)
			{
				analyzer.Count(records.GetFacts(position).Severity, records.GetLineNumber(position), records.GetCreatedAt(position));
			}

			if (analyzer.Fatals > 0)
			{
				this.MetricValue = analyzer.Fatals.ToString("#,##0");
				this.Details =
					$"Fatal and/or critical errors have been detected, with the first occurrence at: {analyzer.FatalFirstOccurredAt:HH:mm:ss}.";
				this.IsAttentionRequired = true;

				// Store the first fatal record's line so users can navigate to it
				if (analyzer.FatalFirstOccurredOnLine != Record.Dummy.LineNumber)
				{
					this.RelatedLineNumbers = new[] { analyzer.FatalFirstOccurredOnLine };
				}
			}
		}
	}
}
