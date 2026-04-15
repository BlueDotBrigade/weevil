namespace BlueDotBrigade.Weevil.Analysis
{
	public enum AnalysisType
	{
		/// <summary>
		/// Flags every record where the regex captures data, annotating each with the extracted value.
		/// </summary>
		DetectData,
		/// <summary>
		/// Flags only the first record where each unique extracted value appears.
		/// </summary>
		DetectFirst,
		/// <summary>
		/// Flags the start and end of consecutive records that share the same extracted value.
		/// </summary>
		DetectStableValues,
		/// <summary>
		/// Flags the record where an extracted value first appears or changes from the previous value.
		/// </summary>
		DetectDataTransition,
		/// <summary>
		/// Flags records where a numeric extracted value decreases compared to the previous record.
		/// </summary>
		DetectFallingEdges,
		/// <summary>
		/// Flags records where a numeric extracted value increases compared to the previous record.
		/// </summary>
		DetectRisingEdges,
		/// <summary>
		/// Flags the first and last record in a block of consecutive records that match the expression.
		/// </summary>
		DetectRepeatingRecords,
		/// <summary>
		/// Flags records preceded by an unexpectedly long gap in timestamps, indicating UI thread delays.
		/// </summary>
		ElapsedTimeUiThread,
		/// <summary>
		/// Calculates the elapsed time between consecutive records based on their timestamps.
		/// </summary>
		ElapsedTime,
		/// <summary>
		/// Flags records whose timestamps go backwards, indicating out-of-order logging.
		/// </summary>
		TemporalAnomaly,
		/// <summary>
		/// Calculates statistics (e.g. mean value, standard deviation, etc.) for the selected records.
		/// </summary>
		Statistical,
	}
}
