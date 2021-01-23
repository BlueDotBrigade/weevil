namespace BlueDotBrigade.Weevil.Filter
{
	using System.Diagnostics;

	[DebuggerDisplay("FilterType={this.Type}, Include={this.Criteria.Include}, Exclude={this.Criteria.Exclude}")]
	internal class Filter
	{
		internal Filter(FilterType type, IFilterCriteria criteria)
		{
			this.Type = type;
			this.Criteria = criteria;
		}

		internal FilterType Type { get; }
		internal IFilterCriteria Criteria { get; }
	}
}
