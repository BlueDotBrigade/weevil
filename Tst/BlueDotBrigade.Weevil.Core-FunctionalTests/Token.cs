namespace BlueDotBrigade.Weevil
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;

	public class Token
	{
		public Token()
		{
			this.Engine = BlueDotBrigade.Weevil.Engine.Surrogate;
			this.FilterType = FilterType.RegularExpression;			
			this.FilterParameters = new Dictionary<string, object>();
			this.Results = ImmutableArray<IRecord>.Empty;
		}
		public IEngine Engine { get; set; }

		public FilterType FilterType { get; set; }

		public FilterCriteria Criteria { get; set; }

		public ImmutableArray<IRecord> Results { get; set; }

		public Dictionary<string, object> FilterParameters { get; set; }
	}
}