namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	[Binding]
	internal sealed class FilterOptionSteps : ReqnrollSteps
	{
		public FilterOptionSteps(Token token) : base (token)
		{
		}

		[When($"the case sensitive option is {X.OnOff}")]
		public void WhenTheCaseSensitiveOptionIsOn(bool isOn)
		{
			
		}

		[When($@"using the ""{X.FilterMode}"" filter mode")]
		public void WhenUsingThePlainTextFilterMode(string filterMode)
		{
			throw new PendingStepException();
		}

		//[When($@"selecting the {R.TextExpression} filter mode")]
		//public void WhenSelectingThePlainTextFilterMode(FilterType expressionType)
		//{
		//	this.Token.FilterType = expressionType == FilterType.PlainText
		//		? FilterType.PlainText
		//		: FilterType.RegularExpression;
		//}

		[When(@"case sensitive filtering has been enabled")]
		public void WhenCaseSensitiveFilteringHasBeenEnabled()
		{
			this.Token.FilterParameters.Add("IsCaseSensitive", true);
		}

		[When($@"the ""{X.ShowSeverityOption}"" filter option is {X.OnOff}")]
		public void WhenTheShowTraceFilterOptionIsOn(string text, bool isOn)
		{
			throw new PendingStepException();
		}
	}
}