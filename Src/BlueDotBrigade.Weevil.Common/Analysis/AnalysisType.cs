namespace BlueDotBrigade.Weevil.Analysis
{
	public enum AnalysisType
	{
		DetectData,
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
	}
}