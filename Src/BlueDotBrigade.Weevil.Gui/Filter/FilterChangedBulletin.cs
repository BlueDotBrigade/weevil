namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.Collections.Generic;

	internal class FilterChangedBulletin
	{
		public FilterChangedBulletin(int selectedRecordCount, int visibleRecordCount, IDictionary<string, object> severityMetrics)
		{
			this.SelectedRecordCount = selectedRecordCount;
			this.VisibleRecordCount = visibleRecordCount;
			this.SeverityMetrics = severityMetrics;
		}

		public int SelectedRecordCount { get; }

		public int VisibleRecordCount { get; }

		public IDictionary<string, object> SeverityMetrics { get; }
	}
}
