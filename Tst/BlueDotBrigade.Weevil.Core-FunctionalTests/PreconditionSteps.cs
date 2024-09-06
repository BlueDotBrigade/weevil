namespace BlueDotBrigade.Weevil.StepDefinitions
{
	using System.Linq.Expressions;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions.PlainText;

	[Binding]
	public sealed class PreconditionSteps
	{
		private readonly Token _token;

		public PreconditionSteps(Token token)
		{
			_token = token;
		}	

		[Given(@"that the default log file is open")]
		public void GivenThatTheDefaultLogFileIsOpen()
		{
			_token.Engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();
		}

		[Given($@"that the `{R.FileName}` log file is open")]
		public void GivenThatTheLogFileIsOpen(string fileName)
		{
			_token.Engine = Engine
				.UsingPath(new Daten().AsFilePath(fileName))
				.Open();
		}

		[Given($@"that the log file is open at `{R.FilePath}`")]
		public void GivenThatTheLogFileIsOpenAt(string filePath)
		{
			_token.Engine = Engine
				.UsingPath(filePath)
				.Open();
		}

		[When($@"selecting the {R.TextExpression} filter mode")]
		public void WhenSelectingThePlainTextFilterMode(FilterType expressionType)
		{
			_token.FilterType = expressionType == FilterType.PlainText
				? FilterType.PlainText
				: FilterType.RegularExpression;
		}

		[When(@"case sensitive filtering has been enabled")]
		public void WhenCaseSensitiveFilteringHasBeenEnabled()
		{
			_token.FilterParameters.Add("IsCaseSensitive", true);
		}

		[When($@"the inclusive filter is applied `{R.AnyText}`")]
		public void WhenTheInclusiveFilterIsApplied(string inclusiveFilter)
		{
			_token.Results = _token.Engine.Filter.Apply(
				_token.FilterType,
				new FilterCriteria(inclusiveFilter, string.Empty, _token.FilterParameters)).Results;
		}
	}
}
