namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	internal class InsightChangedBulletin
	{
		public InsightChangedBulletin(bool hasInsight, int insightNeedingAttention)
		{
			this.HasInsight = hasInsight;
			this.InsightNeedingAttention = insightNeedingAttention;
		}

		public bool HasInsight { get; }

		public int InsightNeedingAttention { get; }

		public bool HasInsightNeedingAttention => this.InsightNeedingAttention > 0;
	}
}
