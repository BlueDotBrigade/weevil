namespace BlueDotBrigade.Weevil.StepDefinitions
{
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

		[Given(@"the default log file was open")]
		public void GivenTheDefaultLogFileWasOpen()
		{
			_token.Engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();
		}

		[Given($@"the log file was open `{R.FileName}`")]
		public void GivenTheLogFileWasOpen(string fileName)
		{
			_token.Engine = Engine
				.UsingPath(new Daten().AsFilePath(fileName))
				.Open();
		}

		[Given($@"the log file was open at `{R.FilePath}`")]
		public void GivenTheLogFileWasOpenAt(string filePath)
		{
			_token.Engine = Engine
				.UsingPath(filePath)
				.Open();
		}

		[When(@"the plain text filtering option is selected")]
		public void WhenThePlainTextFilteringOptionIsSelected()
		{
			_token.FilterType = FilterType.PlainText;
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