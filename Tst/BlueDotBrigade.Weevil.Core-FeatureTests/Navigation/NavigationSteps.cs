namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Generic;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using Reqnroll;

	[Binding]
	internal sealed class NavigationSteps : ReqnrollSteps
	{
		public NavigationSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[When($"selecting the record on line {X.WholeNumber}")]
		public void WhenSelectingTheRecordOnLine(int lineNumber)
		{
			this.Context.Engine.Selector.ClearAll();
			this.Context.Engine.Selector.Select(lineNumber);
		}

		[When("navigating to the next pinned record")]
		public void WhenNavigatingToTheNextPinnedRecord()
		{
			IRecord pinnedRecord = this.Context.Engine.Navigate.NextPin();
			this.Context.Engine.Selector.ClearAll();
			this.Context.Engine.Selector.Select(pinnedRecord);
		}

		[When("navigating to the previous pinned record")]
		public void WhenNavigatingToThePreviousPinnedRecord()
		{
			IRecord pinnedRecord = this.Context.Engine.Navigate.PreviousPin();
			this.Context.Engine.Selector.ClearAll();
			this.Context.Engine.Selector.Select(pinnedRecord);
		}

		[When("selecting the next record")]
		public void WhenSelectingTheNextRecord()
		{
			IRecord currentRecord = this.Context.Engine.Selector.GetSelected().Last();
			var currentIndex = this.Context.Results.IndexOfLineNumber(currentRecord.LineNumber);
			(currentIndex + 1).Should().BeLessThan(this.Context.Results.Length, "a next record must exist after the current selection");
			IRecord nextRecord = this.Context.Results[currentIndex + 1];

			this.Context.Engine.Selector.ClearAll();
			this.Context.Engine.Selector.Select(nextRecord);
		}

		[When("selecting the previous record")]
		public void WhenSelectingThePreviousRecord()
		{
			IRecord currentRecord = this.Context.Engine.Selector.GetSelected().Last();
			var currentIndex = this.Context.Results.IndexOfLineNumber(currentRecord.LineNumber);
			currentIndex.Should().BeGreaterThan(0, "a previous record must exist before the current selection");
			IRecord prevRecord = this.Context.Results[currentIndex - 1];

			this.Context.Engine.Selector.ClearAll();
			this.Context.Engine.Selector.Select(prevRecord);
		}

		[Then($"the selected record line number will be {X.WholeNumber}")]
		public void ThenTheSelectedRecordLineNumberWillBe(int lineNumber)
		{
			this.Context.Engine.Selector.GetSelected()
				.Should()
				.ContainSingle()
				.Which.LineNumber.Should().Be(lineNumber);
		}
	}
}
