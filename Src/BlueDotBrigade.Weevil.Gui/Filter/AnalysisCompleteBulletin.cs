namespace BlueDotBrigade.Weevil.Gui.Filter
{
	internal class AnalysisCompleteBulletin
	{
		public AnalysisCompleteBulletin(int flaggedRecordCount)
		{
			this.FlaggedRecordCount = flaggedRecordCount;
		}

		public int FlaggedRecordCount { get; }
	}
}
