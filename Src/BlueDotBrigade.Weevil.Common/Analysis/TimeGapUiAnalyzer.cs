namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Analyzes timestamped UI-thread records for periods where UI logging appears to have stopped.
	/// </summary>
	/// <remarks>
	/// Untimestamped and non-UI records are ignored and do not replace the previous comparison record.
	/// </remarks>
	public class TimeGapUiAnalyzer : TimeGapAnalyzer
	{
		public override string Key => AnalysisType.ElapsedTimeUiThread.ToString();

		public override string DisplayName => "Measure Elapsed Time (UI Only)";

		protected override string CommentLabel => "ElapsedTimeUi";

		/// <summary>
		/// Measures only the rows that the UI thread wrote, read from their facts, so no record is created (#1150).
		/// </summary>
		protected override bool CanMeasure(RecordList records, int position)
		{
			return records.GetFacts(position).WasGeneratedByUi;
		}

		protected override bool TryGetTolerance(IUserDialog user, out TimeSpan unresponsivenessPeriod)
		{
			return TryGetTolerance(user, "Input Required", "Maximum delay (ms):", out unresponsivenessPeriod);
		}
	}
}
