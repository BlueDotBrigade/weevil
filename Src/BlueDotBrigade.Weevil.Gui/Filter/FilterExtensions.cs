namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil;
	using BlueDotBrigade.Weevil.Filter;

	internal static class FilterExtensions
	{
		internal static Task<bool> FilterAsync(this IEngine engine, FilterType filterType,
			IFilterCriteria filterCriteria, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				var wasFiltered = false;

				// Core keeps the selected records the filter still shows, and an aborted or failed filter changes nothing (#1064).
				try
				{
					engine.Filter.Apply(
						filterType,
						filterCriteria);

					wasFiltered = true;
				}
				catch (FilterAbortedException)
				{
					// The previous results, and their selection, are still shown.
				}

				return wasFiltered;
			}, cancellationToken);
		}
	}
}
