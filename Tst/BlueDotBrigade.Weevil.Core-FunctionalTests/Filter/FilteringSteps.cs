using BlueDotBrigade;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Filter;

namespace BlueDotBrigade.Weevil.Filter
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions.PlainText;

	[Binding]
	public sealed class FilteringSteps
	{
		private readonly Token _token;

		public FilteringSteps(Token token)
		{
			_token = token;
		}

		[When($"entering the include filter: {X.AnyText}")]
		public void WhenEnteringTheIncludeFilter(string text)
		{
			throw new PendingStepException();
		}

		[When($"entering the exclude filter: {X.AnyText}")]
		public void WhenEnteringTheExcludeFilter(string text)
		{
			throw new PendingStepException();
		}

		[When("applying the filters")]
		public void WhenApplyingTheFilters()
		{
			throw new PendingStepException();
		}

		[When($"applying the include filter: {X.AnyText}")]
		public void WhenApplyingTheIncludeFilter(string text)
		{
			throw new PendingStepException();
		}

		[When($"applying the exclude filter: {X.AnyText}")]
		public void WhenApplyingTheExcludeFilter(string text)
		{
			throw new PendingStepException();
		}

		[Then($@"there will be {X.RecordCount} visible records")]
		public void ThenThereWillBeVisibleRecords(int recordCount)
		{
			_token.Engine.Count
				.Should()
				.Be(recordCount);
		}

		[Then($@"all records will include: {X.AnyText}")]
		public void ThenAllRecordsWillInclude(string text)
		{
			_token.Results
				.Should()
				.Contain(s => s.Content.Contains(text, StringComparison.OrdinalIgnoreCase));
		}

		[Then($@"all records will exclude: {X.AnyText}")]
		public void ThenAllRecordsWillExclude(string text)
		{
			_token.Results
				.Should()
				.NotContain(s => s.Content.Contains(text, StringComparison.OrdinalIgnoreCase));
		}

		[Then("each record will include all of the following")]
		public void ThenEachRecordWillIncludeAllOfTheFollowing(string multilineText)
		{
			throw new PendingStepException();
		}

		[Then("each record will exclude all of the following")]
		public void ThenEachRecordWillExcludeAllOfTheFollowing(string multilineText)
		{
			throw new PendingStepException();
		}

		[Then("each record will include either")]
		public void ThenEachRecordWillIncludeEither(string multilineText)
		{
			throw new PendingStepException();
		}

		[Then("no record will contain any of the following")]
		public void ThenNoRecordWillContainAnyOfTheFollowing(string multilineText)
		{
			throw new PendingStepException();
		}


		[When($"pinning the record on line {X.WholeNumber}")]
		public void WhenPinningTheRecordOnLine(int wholeNumber)
		{
			throw new PendingStepException();
		}

		[Then($"line number {X.WholeNumber} will be visible")]
		public void ThenLineNumberWillBeVisible(int lineNumber)
		{
			throw new PendingStepException();
		}

	}
}
