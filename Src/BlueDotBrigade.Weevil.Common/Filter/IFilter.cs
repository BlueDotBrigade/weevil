namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using Data;

	public interface IFilter : IFilterTraits, IFilterBehaviors
	{
		RecordList Results { get; }

		/// <summary>
		/// The elapsed times of <see cref="Results"/>, which each filter replaces (#1016). An aborted filter keeps the previous ones.
		/// </summary>
		ElapsedTimes ElapsedTimes { get; }

		/// <summary>
		/// How long the analyst waited for <see cref="Results"/>: from <see cref="IFilterBehaviors.Apply"/> until the selected records it shows were kept,
		/// including the wait for the filter it replaced to stop (#1248).
		/// </summary>
		/// <remarks>
		/// Set once the <see cref="IFilterBehaviors.ResultsChanged"/> handlers return.
		/// </remarks>
		TimeSpan FilterExecutionTime { get; }
	}
}