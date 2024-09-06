using BlueDotBrigade;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Filter;

namespace BlueDotBrigade.Weevil.Filter
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions.PlainText;

	[Binding]
	public sealed class ResultsSteps
	{
		private readonly Token _token;

		public ResultsSteps(Token token)
		{
			_token = token;
		}

		[Then($@"there will be {R.RecordCount} visible records")]
		public void ThenThereWillBeVisibleRecords(int recordCount)
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