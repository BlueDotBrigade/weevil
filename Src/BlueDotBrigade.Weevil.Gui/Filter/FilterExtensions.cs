namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.Collections.Immutable;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;

	internal static class FilterExtensions
	{
		internal static Task<bool> FilterAsync(this IEngine engine, FilterType filterType,
			FilterCriteria filterCriteria, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				var wasFiltered = false;

				ImmutableArray<IRecord> previousRecordSelection = engine.Selector.ClearAll();

				try
				{
					engine.Filter.Apply(
						filterType,
						filterCriteria);

					var newRecordSelection = engine.Filter.Results
						.Where(a => previousRecordSelection.Any(b => b.LineNumber == a.LineNumber)).ToList();

					engine.Selector.Select(newRecordSelection);

					wasFiltered = true;
				}
				catch (FilterAbortedException)
				{
					engine.Selector.Select(previousRecordSelection);
				}

				return wasFiltered;
			}, cancellationToken);
		}
	}
}
