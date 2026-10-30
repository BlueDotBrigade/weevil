namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Immutable;
	using Data;

	public interface IFilter : IFilterTraits, IFilterBehaviors
	{
		RecordList Results { get; }

		/// <summary>
		/// The elapsed times of <see cref="Results"/>, which each filter replaces (#1016). An aborted filter keeps the previous ones.
		/// </summary>
		ElapsedTimes ElapsedTimes { get; }

		TimeSpan FilterExecutionTime { get; }
	}
}