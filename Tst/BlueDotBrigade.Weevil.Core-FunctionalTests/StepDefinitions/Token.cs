namespace BlueDotBrigade.Weevil.StepDefinitions
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;

	internal class Token
	{
		public Token()
		{
			this.Engine = BlueDotBrigade.Weevil.Engine.Surrogate;
			this.Results = ImmutableArray<IRecord>.Empty;
			this.FilterType = FilterType.RegularExpression;
			this.FilterParameters = new Dictionary<string, object>();
		}

		public IEngine Engine { get; set; }

		public FilterType FilterType { get; set; }

		public ImmutableArray<IRecord> Results { get; set; }

		public Dictionary<string, object> FilterParameters { get; set; }
	}
}
