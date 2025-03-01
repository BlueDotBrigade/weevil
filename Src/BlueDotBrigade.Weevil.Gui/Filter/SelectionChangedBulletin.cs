namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using BlueDotBrigade.Weevil.Data;

	internal class SelectionChangedBulletin
	{
		public SelectionChangedBulletin()
		{
			this.LineNumber = 0;
			this.SelectionPeriod = Metadata.ElapsedTimeUnknown;
			this.SelectedRecordCount = 0;
			this.RegionName = string.Empty;
			this.SectionName = string.Empty;
		}

		public int LineNumber { get; init; }

		public TimeSpan SelectionPeriod { get; init; }

		public int SelectedRecordCount { get; init; }

		public string SectionName { get; init; }

		public string RegionName { get; init; }

		public bool HasContext => !string.IsNullOrWhiteSpace(this.RegionName) || !string.IsNullOrWhiteSpace(this.SectionName);
	}
}
