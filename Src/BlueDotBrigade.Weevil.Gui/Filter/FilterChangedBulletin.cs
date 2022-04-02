namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;

	internal class FilterChangedBulletin
	{
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

		public int SelectedRecordCount { get; }

		public int VisibleRecordCount { get; }

		public IDictionary<string, object> SeverityMetrics { get; }

		public TimeSpan ExecutionTime { get; }
	}
}
