namespace BlueDotBrigade.Weevil
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions.PlainText;

	[Binding]
	public sealed class StatusBarSteps
	{
		private readonly Token _token;

		public StatusBarSteps(Token token)
		{
			_token = token;
		}

		[Then($@"the record count will be {R.RecordCount}")]
		public void ThenTheRecordCountWillBe(int recordCount)
		{
			_token.Engine.Count
				.Should()
				.Be(recordCount);
		}

		[Then($@"all records will include `{R.AnyText}`")]
		public void ThenAllRecordsWillInclude(string text)
		{
			_token.Results
				.Should()
				.Contain(s => s.Content.Contains(text, StringComparison.OrdinalIgnoreCase));
		}
	}
}