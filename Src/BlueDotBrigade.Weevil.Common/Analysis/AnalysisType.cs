namespace BlueDotBrigade.Weevil.Analysis
{
	public enum AnalysisType
	{
		/// <summary>
		/// Flags every record that matches the expression and annotates it with extracted named-group values.
		/// </summary>
		DetectData,
		/// <summary>
		/// Flags only the first record where each unique extracted value appears.
		/// </summary>
		FirstOccurrence,
		/// <summary>
		/// Flags only the last record where each unique extracted value appears.
		/// </summary>
		LastOccurrence,
		/// <summary>
		/// Flags the first and last records in each maximal contiguous run where matching records
		/// share the same extracted value.
		/// </summary>
		/// <remarks>
		/// A record that does not provide a value for a captured key terminates that key's active run.
		/// A single-record run contributes one flagged record because its start and end are the same record.
		/// </remarks>
		StableValueRuns,
		/// <summary>
		/// Flags each record where an extracted value first appears or changes from the previous matching value.
		/// </summary>
		/// <remarks>
		/// Records that do not provide a value for a captured key are ignored and do not reset its previous value.
		/// </remarks>
		StateTransitions,
		/// <summary>
		/// Flags the record immediately before the first decrease in each falling run.
		/// </summary>
		/// <remarks>
		/// Records that do not capture a value for the key are ignored. An equal or increasing
		/// value ends that key's current falling run.
		/// </remarks>
		DetectFallingEdges,
		/// <summary>
		/// Flags the record immediately before the first increase in each rising run.
		/// </summary>
		/// <remarks>
		/// Records that do not capture a value for the key are ignored. An equal or decreasing
		/// value ends that key's current rising run.
		/// </remarks>
		DetectRisingEdges,
		/// <summary>
		/// Flags records whose extracted numeric values satisfy a user-provided threshold comparison.
		/// </summary>
		/// <remarks>
		/// Supported comparisons are greater-than (<c>&gt;</c>), greater-than-or-equal (<c>&gt;=</c>),
		/// less-than (<c>&lt;</c>), and less-than-or-equal (<c>&lt;=</c>).
		/// </remarks>
		ThresholdCrossings,
		/// <summary>
		/// Flags the first and last records in each maximal contiguous run of two or more matching records.
		/// </summary>
		/// <remarks>
		/// A non-matching record terminates the current run.
		/// </remarks>
		MatchingRecordRuns,
		/// <summary>
		/// Flags timestamped UI-thread records preceded by an unexpectedly long gap between timestamped UI records.
		/// </summary>
		/// <remarks>
		/// Untimestamped and non-UI records are ignored and do not replace the previous comparison record.
		/// </remarks>
		ElapsedTimeUiThread,
		/// <summary>
		/// Calculates elapsed time between consecutive timestamped records.
		/// </summary>
		/// <remarks>
		/// Untimestamped records are ignored and do not replace the previous comparison record.
		/// </remarks>
		ElapsedTime,
		/// <summary>
		/// Flags records whose timestamps move backwards beyond the configured tolerance.
		/// </summary>
		OutOfOrderTimestamps,
		/// <summary>
		/// Calculates statistics (e.g. mean value, standard deviation, etc.) for the selected records.
		/// </summary>
		Statistical,
	}
}
