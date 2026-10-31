namespace BlueDotBrigade.Weevil.Selection
{
	using Reqnroll;

	[Binding]
	internal sealed class SelectingSteps : ReqnrollSteps
	{
		public SelectingSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[When($"selecting only lines {X.WholeNumber} to {X.WholeNumber}")]
		public void WhenSelectingOnlyLines(int anchorLineNumber, int lineNumber)
		{
			this.Context.Engine.Selector.SelectOnly(anchorLineNumber, lineNumber);
		}

		[When($"selecting only from the anchor to line {X.WholeNumber}")]
		public void WhenSelectingOnlyFromTheAnchorToLine(int lineNumber)
		{
			this.Context.Engine.Selector.SelectOnly(this.Context.Engine.Selector.AnchorLineNumber, lineNumber);
		}

		[When($"adding lines {X.WholeNumber} to {X.WholeNumber} to the selection")]
		public void WhenAddingLinesToTheSelection(int anchorLineNumber, int lineNumber)
		{
			this.Context.Engine.Selector.SelectRange(anchorLineNumber, lineNumber);
		}

		[When($"toggling the selection of line {X.WholeNumber}")]
		public void WhenTogglingTheSelectionOfLine(int lineNumber)
		{
			this.Context.Engine.Selector.Toggle(lineNumber);
		}

		[When("selecting every record")]
		public void WhenSelectingEveryRecord()
		{
			this.Context.Engine.Selector.SelectAll();
		}

		[Given($"line {X.WholeNumber} has the comment: {X.AnyText}")]
		public void GivenLineHasTheComment(int lineNumber, string comment)
		{
			this.Context.Engine.Annotations.SetComment(lineNumber, comment);
		}

		[When($"commenting the selected records: {X.AnyText}")]
		public void WhenCommentingTheSelectedRecords(string comment)
		{
			this.Context.Engine.Selector.SetComment(comment, allowOverwrite: false);
		}

		[When($"commenting the selected records, replacing their comments: {X.AnyText}")]
		public void WhenCommentingTheSelectedRecordsReplacingTheirComments(string comment)
		{
			this.Context.Engine.Selector.SetComment(comment, allowOverwrite: true);
		}

		[When("commenting the selected records with only white space")]
		public void WhenCommentingTheSelectedRecordsWithOnlyWhiteSpace()
		{
			this.Context.Engine.Selector.SetComment("   ", allowOverwrite: false);
		}

		[Then($"the record on line {X.WholeNumber} will have no comment")]
		public void ThenTheRecordOnLineWillHaveNoComment(int lineNumber)
		{
			this.Context.Engine.Annotations.GetComment(lineNumber).Should().BeEmpty();
		}

		[Given($"line {X.WholeNumber} is pinned")]
		public void GivenLineIsPinned(int lineNumber)
		{
			this.Context.Engine.Annotations.SetPinned(lineNumber, true);
		}

		[When("toggling the pins of the selected records")]
		public void WhenTogglingThePinsOfTheSelectedRecords()
		{
			this.Context.Engine.Selector.ToggleIsPinned();
		}

		[Then($"the record on line {X.WholeNumber} will be pinned")]
		public void ThenTheRecordOnLineWillBePinned(int lineNumber)
		{
			this.Context.Engine.Annotations.IsPinned(lineNumber).Should().BeTrue();
		}

		[Then($"the record on line {X.WholeNumber} will not be pinned")]
		public void ThenTheRecordOnLineWillNotBePinned(int lineNumber)
		{
			this.Context.Engine.Annotations.IsPinned(lineNumber).Should().BeFalse();
		}

		[When("reloading the log")]
		public void WhenReloadingTheLog()
		{
			this.Context.Engine.Reload();
		}

		[Then($"the anchor will be line {X.WholeNumber}")]
		public void ThenTheAnchorWillBeLine(int lineNumber)
		{
			this.Context.Engine.Selector.AnchorLineNumber.Should().Be(lineNumber);
		}

		[Then("there will be no anchor")]
		public void ThenThereWillBeNoAnchor()
		{
			this.Context.Engine.Selector.AnchorLineNumber.Should().Be(-1);
		}
	}
}
