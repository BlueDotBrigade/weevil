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
