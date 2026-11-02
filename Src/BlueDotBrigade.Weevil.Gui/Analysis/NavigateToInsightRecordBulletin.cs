namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System.Collections.Generic;

	/// <summary>
	/// Bulletin requesting navigation to records related to an insight.
	/// </summary>
	internal class NavigateToInsightRecordBulletin
	{
		public NavigateToInsightRecordBulletin(IReadOnlyList<int> relatedLineNumbers)
		{
			this.RelatedLineNumbers = relatedLineNumbers;
		}

		/// <summary>
		/// The line numbers of all records related to the insight that require user attention.
		/// </summary>
		public IReadOnlyList<int> RelatedLineNumbers { get; }
	}
}
