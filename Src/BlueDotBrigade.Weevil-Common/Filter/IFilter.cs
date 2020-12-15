namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Immutable;
	using Data;

	public interface IFilter : IFilterTraits, IFilterBehaviors
	{
		ImmutableArray<IRecord> Results { get; }
		TimeSpan FilterExecutionTime { get; }
	}
}