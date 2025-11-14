namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// Bulletin requesting navigation to a record related to an insight.
	/// </summary>
	internal class NavigateToInsightRecordBulletin
	{
		public NavigateToInsightRecordBulletin(IRecord record)
		{
			this.Record = record;
		}

		/// <summary>
		/// The record to navigate to in the main window.
		/// </summary>
		public IRecord Record { get; }
	}
}
