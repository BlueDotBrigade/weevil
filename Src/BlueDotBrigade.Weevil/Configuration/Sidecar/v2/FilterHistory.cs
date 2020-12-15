namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	using System.Collections.Generic;

	public class FilterHistory
	{
		public FilterHistory()
		{
			this.InclusiveFilters = new List<Filter>
			{
				{ new Filter { SortOrder= 1, Value = "Abc"}}
			};
			this.ExclusiveFilters = new List<Filter>
			{
				{ new Filter { SortOrder= 26, Value = "Xyz"}}
			};
		}

		public List<Filter> InclusiveFilters { get; set; }
		public List<Filter> ExclusiveFilters { get; set; }
	}
}