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
			"Represents the number of critical or fatal errors that have been logged.")
		{
			// nothing to do
		}

		protected override void OnRefresh(ImmutableArray<IRecord> records)
		{
			var analyzer = new CriticalErrorsAnalyzer();
			analyzer.Analyze(records, string.Empty, null);

			if (analyzer.Count > 0)
			{
				this.MetricValue = analyzer.Count.ToString();
				this.Details =
					$"Critical/fatal errors have been detected, with the first occurrence at: {analyzer.FirstOccurrence.CreatedAt:HH:mm:ss}.";
				this.IsAttentionNeeded = true;
			}
		}
	}
}
