namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil;
	using BlueDotBrigade.Weevil.Filter;

	internal static class FilterExtensions
	{
		internal static Task<bool> FilterAsync(this IEngine engine, FilterType filterType,
			IFilterCriteria filterCriteria, CancellationToken cancellationToken)
		{
			return RunAsync(() => engine.Filter.Apply(filterType, filterCriteria), cancellationToken);
		}

		/// <summary>
		/// Runs the current filter again (F5) on a background thread (#1245).
		/// </summary>
		/// <remarks>
		/// <see cref="IFilterBehaviors.ReApply"/>, not <see cref="IFilterBehaviors.Apply"/>: it keeps the elapsed times the filter compared (#1224), and records no telemetry.
		/// </remarks>
		internal static Task<bool> ReApplyAsync(this IEngine engine, CancellationToken cancellationToken)
		{
			return RunAsync(() => engine.Filter.ReApply(), cancellationToken);
		}

		private static Task<bool> RunAsync(Func<IFilter> filter, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				// Core keeps the selected records the filter still shows, and an aborted or failed filter changes nothing (#1064).
				try
				{
					filter();

					return true;
				}
				catch (FilterAbortedException)
				{
					// The previous results, and their selection, are still shown.
					return false;
				}
			}, cancellationToken);
		}
	}
}
