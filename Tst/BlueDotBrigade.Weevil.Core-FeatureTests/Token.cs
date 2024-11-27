namespace BlueDotBrigade.Weevil
{
	using System.Collections.Immutable;
	using System.Security;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using NSubstitute.Extensions;

	internal class Token
	{
		public Token()
		{
			this.Engine = BlueDotBrigade.Weevil.Engine.Surrogate;

			this.IncludeFilter = string.Empty;
			this.ExcludeFilter = string.Empty;

			this.FilterType = FilterType.RegularExpression;

			this.Configuration = new Dictionary<string, object>()
			{
				{ "IncludePinned", true },
				{ "IsCaseSensitive", true },
				{ "HideDebugRecords", false },
				{ "HideTraceRecords", false },
			};
		}

		public IEngine Engine { get; set; }

		public string IncludeFilter { private get; set; }

		public string ExcludeFilter { private get; set; }

		public FilterType FilterType { get; set; }

		public Dictionary<string, object> Configuration { get; set; }

		public FilterCriteria FilterCriteria => new FilterCriteria(
			this.IncludeFilter, 
			this.ExcludeFilter, 
			this.Configuration);

		public ImmutableArray<IRecord> Results => Engine.Filter.Results;
	}
}
