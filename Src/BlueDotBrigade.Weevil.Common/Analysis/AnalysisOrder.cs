namespace BlueDotBrigade.Weevil.Analysis
{
	public enum AnalysisOrder 
	{
		/// <summary>
		/// Records are analyzed from the most recent timestamp to the oldest timestamp.
		/// </summary>
		Descending,
		/// <summary>
		/// Records are analyzed from oldest timestamp to latest timestamp.
		/// </summary>
		Ascending,
	}
}