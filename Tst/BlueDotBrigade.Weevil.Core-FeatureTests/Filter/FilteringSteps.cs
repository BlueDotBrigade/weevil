using BlueDotBrigade;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Filter;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Filter.Expressions.PlainText;

using static System.Net.Mime.MediaTypeNames;

[Binding]
internal sealed class FilteringSteps : ReqnrollSteps
{
	public FilteringSteps(Token token) : base(token)
	{
		// nothing to do
	}

	/// <seealso cref="WhenApplyingTheIncludeFilter"/>
	[When($"using the include filter: {X.AnyText}")]
	public void WhenUsingTheIncludeFilter(string includeFilter)
	{
		this.Context.IncludeFilter = includeFilter;
	}

	/// <seealso cref="WhenApplyingTheExcludeFilter"/>
	[When($"using the exclude filter: {X.AnyText}")]
	public void WhenUsingTheExcludeFilter(string excludeFilter)
	{
		this.Context.ExcludeFilter = excludeFilter;
	}

	[When("applying the filters")]
	public void WhenApplyingTheFilters()
	{
		this.Context.Engine.Filter.Apply(
			this.Context.FilterType,
			this.Context.FilterCriteria);
	}

	/// <seealso cref="WhenEnteringTheIncludeFilter"/>
	[When($"applying the include filter: {X.AnyText}")]
	public void WhenApplyingTheIncludeFilter(string includeFilter)
	{
		this.Context.IncludeFilter = includeFilter;

		this.Context.Engine.Filter.Apply(
			this.Context.FilterType,
			this.Context.FilterCriteria);
	}

	/// <seealso cref="WhenEnteringTheExcludeFilter"/>
	[When($"applying the exclude filter: {X.AnyText}")]
	public void WhenApplyingTheExcludeFilter(string excludeFilter)
	{
		this.Context.ExcludeFilter = excludeFilter;

		this.Context.Engine.Filter.Apply(
			this.Context.FilterType,
			this.Context.FilterCriteria);
	}

	[Then($@"there will be {X.WholeNumber} matching records")]
	public void ThenThereWillBeMatchingRecords(int recordCount)
	{
		this.Context.Results.Length
			.Should()
			.Be(recordCount);
	}

	[Then($@"all records will include: {X.AnyText}")]
	public void ThenAllRecordsWillInclude(string text)
	{
		this.Context.Results
			.Should()
			.Contain(s => s.Content.Contains(text));
	}

	[Then($@"all records will exclude: {X.AnyText}")]
	public void ThenAllRecordsWillExclude(string text)
	{
		this.Context.Results
			.Should()
			.NotContain(s => s.Content.Contains(text));
	}

	[Then("each record will include all of the following")]
	public void ThenEachRecordWillIncludeAllOfTheFollowing(string multilineText)
	{
		var textValues = multilineText.Split(
			new[] { Environment.NewLine },
			StringSplitOptions.RemoveEmptyEntries);

		foreach (var record in this.Context.Results)
		{
			foreach (var text in textValues)
			{
				text.Should().Contain(text);
			}
		}
	}

	[Then("each record will exclude all of the following")]
	public void ThenEachRecordWillExcludeAllOfTheFollowing(string multilineText)
	{
		var textValues = multilineText.Split(
			new[] { Environment.NewLine },
			StringSplitOptions.RemoveEmptyEntries);

		foreach (var text in textValues)
		{
			this.Context.Results
				.Should()
				.NotContain(s => s.Content.Contains(text));
		}
	}

	[Then("each record will include either")]
	public void ThenEachRecordWillIncludeEither(string multilineText)
	{
		var textValues = multilineText.Split(
			new[] { Environment.NewLine }, 
			StringSplitOptions.RemoveEmptyEntries);

		foreach (var text in textValues)
		{
			this.Context.Results
				.Should()
				.Contain(s => s.Content.Contains(text));
		}
	}

	[Then("no record will contain any of the following")]
	public void ThenNoRecordWillContainAnyOfTheFollowing(string multilineText)
	{
		var textValues = multilineText.Split(
			new[] { Environment.NewLine },
			StringSplitOptions.RemoveEmptyEntries);

		foreach (var text in textValues)
		{
			this.Context.Results
				.Should()
				.NotContain(s => s.Content.Contains(text));
		}
	}

	[When($"pinning the record on line {X.WholeNumber}")]
	public void WhenPinningTheRecordOnLine(int lineNumber)
	{
		var index = this.Context.Engine.Records.IndexOfLineNumber(lineNumber);
		this.Context.Engine.Records[index].Metadata.IsPinned = true;
	}

	[When($"unpinning the record on line {X.WholeNumber}")]
	public void WhenUnpinningTheRecordOnLine(int lineNumber)
	{
		var index = this.Context.Engine.Records.IndexOfLineNumber(lineNumber);
		this.Context.Engine.Records[index].Metadata.IsPinned = false;
	}

	[When($"bookmarking the record on line {X.WholeNumber}")]
	public void WhenBookmarkingTheRecordOnLine(int lineNumber)
	{
		this.Context.Engine.Bookmarks.CreateFromSelection(0, string.Empty, lineNumber);
	}

	[Then($"line number {X.WholeNumber} will be visible")]
	public void ThenLineNumberWillBeVisible(int lineNumber)
	{
		var index = this.Context.Results.IndexOfLineNumber(lineNumber);

		index.Should().BeGreaterOrEqualTo(0);
	}
}