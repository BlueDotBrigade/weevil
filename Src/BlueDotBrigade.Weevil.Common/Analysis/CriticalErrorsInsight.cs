namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;
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

		protected override void OnRefresh(ImmutableArray<IRecord> records)
		{
			var analyzer = new SeverityMetrics();

			foreach (var record in records)
			{
				analyzer.Count(record);
			}

			if (analyzer.Fatals > 0)
			{
				this.MetricValue = analyzer.Fatals.ToString("#,##0");
				this.Details =
					$"Fatal and/or critical errors have been detected, with the first occurrence at: {analyzer.FatalFirstOccurredAt.CreatedAt:HH:mm:ss}.";
				this.IsAttentionRequired = true;

				// Store the first fatal record so users can navigate to it
				if (analyzer.FatalFirstOccurredAt != null)
				{
					this.RelatedRecords = ImmutableArray.Create(analyzer.FatalFirstOccurredAt);
				}
			}
		}
	}
}
