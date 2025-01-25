namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using BlueDotBrigade.Weevil.Data;

	internal class SelectionChangedBulletin
	{
		public SelectionChangedBulletin()
		{
			this.SelectionPeriod = Metadata.ElapsedTimeUnknown;
			this.SelectedRecordCount = 0;
			this.CurrentRegion = string.Empty;
			this.CurrentSection = string.Empty;
		}
		public TimeSpan SelectionPeriod { get; init; }

		public int SelectedRecordCount { get; init; }

		public string CurrentSection { get; init; }

		public string CurrentRegion { get; init; }

		public bool HasContext => !string.IsNullOrWhiteSpace(this.CurrentRegion) || !string.IsNullOrWhiteSpace(this.CurrentSection);
	}
}
