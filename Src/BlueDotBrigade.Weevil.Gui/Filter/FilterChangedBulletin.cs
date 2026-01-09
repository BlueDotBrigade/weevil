namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;

	internal class FilterChangedBulletin
	{
		public FilterChangedBulletin()
		{
			this.SelectedRecordCount = 0;
			this.VisibleRecordCount = 0;
			this.ExecutionTime = TimeSpan.Zero;
			this.SeverityMetrics = new Dictionary<string, object>();
			this.BookmarkCount = 0;
			this.RegionCount = 0;
		}
		public FilterChangedBulletin(
			int selectedRecordCount, 
			int visibleRecordCount, 
			IDictionary<string, object> severityMetrics, 
			TimeSpan executionTime)
		{
			this.SelectedRecordCount = selectedRecordCount;
			this.VisibleRecordCount = visibleRecordCount;
			this.SeverityMetrics = severityMetrics;
			this.ExecutionTime = executionTime;
		}

		public int SelectedRecordCount { get; init; }

		public int VisibleRecordCount { get; init; }

		public IDictionary<string, object> SeverityMetrics { get; init; }

		public TimeSpan ExecutionTime { get; init; }

		public int BookmarkCount { get; init; }

		public int RegionCount { get; init; }
	}
}
