namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Data;

	[Binding]
	internal sealed class FilterOptionSteps : ReqnrollSteps
	{
		public FilterOptionSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[When($"the case sensitive option is {X.OnOff}")]
		public void WhenTheCaseSensitiveOptionIsOn(bool isOn)
		{
			this.Context.Configuration["IsCaseSensitive"] = isOn;
		}

		[When($@"using the ""{X.FilterMode}"" filter mode")]
		public void WhenUsingThePlainTextFilterMode(FilterType filterMode)
		{
			this.Context.FilterType = filterMode;
		}

		[When(@"case sensitive filtering has been enabled")]
		public void WhenCaseSensitiveFilteringHasBeenEnabled()
		{
			this.Context.Configuration.Add("IsCaseSensitive", true);
		}

		// TODO: Update UI to use the terms "Show Debug" and "Show Trace"
		[When($@"the ""{X.ShowSeverityOption}"" filter option is {X.OnOff}")]
		public void WhenTheShowTraceFilterOptionIsOn(SeverityType severityOption, bool isOn)
		{
			switch (severityOption)
			{
				case SeverityType.Debug:
					this.Context.Configuration["HideDebugRecords"] = !isOn;
					break;

				case SeverityType.Verbose:
					this.Context.Configuration["HideTraceRecords"] = !isOn;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(severityOption), severityOption, $"Severity option was expected to be either: {X.ShowSeverityOption}");
			}
		}
	}
}