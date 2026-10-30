namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// Bulletin requesting navigation to records related to an insight.
	/// </summary>
	internal class NavigateToInsightRecordBulletin
	{
		public NavigateToInsightRecordBulletin(RecordList relatedRecords)
		{
			this.RelatedRecords = relatedRecords;
		}

		/// <summary>
		/// All records related to the insight that require user attention.
		/// </summary>
		public RecordList RelatedRecords { get; }
	}
}
