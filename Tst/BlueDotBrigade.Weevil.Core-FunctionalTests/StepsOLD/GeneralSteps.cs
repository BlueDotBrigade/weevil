namespace BlueDotBrigade.Weevil.StepDefinitions
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions.PlainText;

	[Binding]
	public sealed class GeneralSteps
	{
		private readonly Token _token;

		public GeneralSteps()
		{
			_token = new Token();
		}

		[Given(@"that the default log file is open")]
		public void GivenThatTheDefaultLogFileIsOpen()
		{
			_token.Engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();
		}

		[Given($@"that the following log file is open `{A.FileName}`")]
		public void GivenTheUserHasOpenedThisLog(string filename)
		{
			_token.Engine = Engine
				.UsingPath(new Daten().AsFilePath(filename))
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

		[When($@"the inclusive filter is applied `{A.AnyText}`")]
		public void WhenTheInclusiveFilterIsApplied(string inclusiveFilter)
		{
			_token.Results = _token.Engine.Filter.Apply(
				_token.FilterType,
				new FilterCriteria(inclusiveFilter, string.Empty, _token.FilterParameters)).Results;
		}

		[Then($@"the record count will be {A.RecordCount}")]
		public void ThenTheRecordCountWillBe(int recordCount)
		{
			_token.Engine.Count
				.Should()
				.Be(recordCount);
		}

		[Then($@"all records will include `{A.AnyText}`")]
		public void ThenAllRecordsWillInclude(string text)
		{
			_token.Results
				.Should()
				.Contain(s => s.Content.Contains(text, StringComparison.OrdinalIgnoreCase));
		}
	}
}