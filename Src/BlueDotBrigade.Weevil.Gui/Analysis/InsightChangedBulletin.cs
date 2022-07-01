namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	internal class InsightChangedBulletin
	{
		public InsightChangedBulletin()
		{
			this.HasInsight = false;
			this.InsightNeedingAttention = 0;
		}

		public bool HasInsight { get; init; }

		public int InsightNeedingAttention { get; init; }

		public bool HasInsightNeedingAttention => this.InsightNeedingAttention > 0;
	}
}
