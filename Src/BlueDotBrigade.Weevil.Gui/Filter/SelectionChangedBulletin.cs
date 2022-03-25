namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;

	internal class SelectionChangedBulletin
	{
		public SelectionChangedBulletin(int selectedRecordCount, TimeSpan selectionPeriod, string currentSection)
		{
			this.SelectedRecordCount = selectedRecordCount;
			this.SelectionPeriod = selectionPeriod;
			this.CurrentSection = currentSection;
		}

		public TimeSpan SelectionPeriod { get; }

		public int SelectedRecordCount { get; }

		public string CurrentSection { get; }
	}
}
