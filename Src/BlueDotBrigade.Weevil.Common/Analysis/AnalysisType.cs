namespace BlueDotBrigade.Weevil.Analysis
{
	public enum AnalysisType
	{
               DetectData,
               DetectFirst,
               DetectDataTransition,
		DetectFallingEdges,
		DetectRisingEdges,
		/// <summary>
		/// Identifies the first & last record in a block of repeating of repeating records.
		/// </summary>
		DetectRepeatingRecords,
		ElapsedTimeUiThread,
		ElapsedTime,
		TemporalAnomaly,
		/// <summary>
		/// Calculates statistics (e.g. mean value, standard deviation, etc.) for the selected records.
		/// </summary>
		Statistical,
	}
}