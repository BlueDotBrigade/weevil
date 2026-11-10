namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;

	public interface IInsight
	{
		 string Title { get; }

		 string MetricValue { get; }

		 string MetricUnit { get; }

		 string Details { get; }

		 bool IsAttentionRequired { get; }

		 /// <summary>
		 /// Gets the line numbers of the records that are related to this insight.
		 /// </summary>
		 /// <remarks>
		 /// When the insight has detected a problem, these are the records that require user attention,
		 /// with the first being the primary record of interest.
		 /// They are line numbers, not records, so an insight keeps none of a log's content in memory (#1139).
		 /// </remarks>
		 IReadOnlyList<int> RelatedLineNumbers { get; }

		 /// <remarks>
		 /// Keep line numbers, not records: <paramref name="records"/> is valid only during the call.
		 /// In disk mode, reading a record's content reads the log, and can throw an <see cref="System.IO.IOException"/> (#1025).
		 /// </remarks>
		 void Refresh(RecordList records);
	}
}
