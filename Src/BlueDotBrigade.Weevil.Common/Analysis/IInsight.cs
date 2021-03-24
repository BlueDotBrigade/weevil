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

		 void Refresh(ImmutableArray<IRecord> records);
	}
}
