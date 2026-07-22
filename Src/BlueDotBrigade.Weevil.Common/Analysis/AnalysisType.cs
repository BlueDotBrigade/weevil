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
		/// Flags the start and end boundaries of runs where consecutive records share the same extracted value.
		/// </summary>
		StableValueRuns,
		/// <summary>
		/// Flags each record where an extracted value first appears or changes from the previous value.
		/// </summary>
		StateTransitions,
		/// <summary>
		/// Flags records where a numeric extracted value decreases compared to the previous record.
		/// </summary>
		DetectFallingEdges,
		/// <summary>
		/// Flags records where a numeric extracted value increases compared to the previous record.
		/// </summary>
		DetectRisingEdges,
		/// <summary>
		/// Flags records whose extracted numeric values are above or below a user-provided threshold.
		/// </summary>
		ThresholdCrossings,
		/// <summary>
		/// Flags the first and last record in each run of two or more consecutive records that match the expression.
		/// </summary>
		MatchingRecordRuns,
		/// <summary>
		/// Flags records preceded by an unexpectedly long gap in timestamps, indicating UI thread delays.
		/// </summary>
		ElapsedTimeUiThread,
		/// <summary>
		/// Calculates the elapsed time between consecutive records based on their timestamps.
		/// </summary>
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
