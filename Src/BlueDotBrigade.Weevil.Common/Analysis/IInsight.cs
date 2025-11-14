namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	public interface IInsight
	{
		 string Title { get; }

		 string MetricValue { get; }

		 string MetricUnit { get; }

		 string Details { get; }

		 bool IsAttentionRequired { get; }

		 /// <summary>
		 /// Gets the collection of records that are related to this insight.
		 /// </summary>
		 /// <remarks>
		 /// When the insight has detected a problem, this collection contains the records
		 /// that require user attention, with the first record being the primary record of interest.
		 /// </remarks>
		 ImmutableArray<IRecord> RelatedRecords { get; }

		 void Refresh(ImmutableArray<IRecord> records);
	}
}
