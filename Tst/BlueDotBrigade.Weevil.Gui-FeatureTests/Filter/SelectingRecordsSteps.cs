namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.Windows.Input;

	/// <summary>
	/// Selects records as the grid does: it passes clicks and keys to the filter's view-model, which never lets WPF select a row (#1064).
	/// </summary>
	[Binding]
	internal sealed class SelectingRecordsSteps : ReqnrollSteps
	{
		// The keys these steps press do not depend on the number of rows on screen.
		private const int PageSize = 20;

		public SelectingRecordsSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[Given($"lines {X.WholeNumber} to {X.WholeNumber} are selected")]
		public void GivenLinesAreSelected(int firstLineNumber, int lastLineNumber)
		{
			Click(firstLineNumber, ModifierKeys.None);
			Click(lastLineNumber, ModifierKeys.Shift);
		}

		[Given($@"line {X.WholeNumber} has the comment ""{X.AnyText}""")]
		public void GivenLineHasTheComment(int lineNumber, string comment)
		{
			// As the row's comment box sends it.
			GetRow(lineNumber).Comment = comment;
		}

		[When($"clicking line {X.WholeNumber}")]
		public void WhenClickingLine(int lineNumber)
		{
			Click(lineNumber, ModifierKeys.None);
		}

		[When($@"Shift\+clicking line {X.WholeNumber}")]
		public void WhenShiftClickingLine(int lineNumber)
		{
			Click(lineNumber, ModifierKeys.Shift);
		}

		[When($@"Ctrl\+clicking line {X.WholeNumber}")]
		public void WhenCtrlClickingLine(int lineNumber)
		{
			Click(lineNumber, ModifierKeys.Control);
		}

		// A Cucumber expression, in which "+" is plain text: Reqnroll reads a pattern as a regex only when it has a group such as (\d+).
		[When("pressing Shift+End")]
		public void WhenPressingShiftEnd()
		{
			this.Context.Filter.PressKey(Key.End, ModifierKeys.Shift, PageSize, isOnRow: true);
		}

		[When($"going to the next comment {X.WholeNumber} times")]
		public void WhenGoingToTheNextComment(int times)
		{
			for (var i = 0; i < times; i++)
			{
				this.Context.Filter.GoToNextComment();
			}
		}

		[Then($"the status bar will show {X.WholeNumber} selected records?")]
		public void ThenTheStatusBarWillShowSelectedRecords(int recordCount)
		{
			this.Context.StatusBar.SelectionDetails.SelectedRecordCount.Should().Be(recordCount);
		}

		[Then($"line number {X.WholeNumber} will be selected")]
		public void ThenLineNumberWillBeSelected(int lineNumber)
		{
			GetRow(lineNumber).IsSelected.Should().BeTrue();
		}

		[Then($"the grid will show and focus line {X.WholeNumber}")]
		public void ThenTheGridWillShowAndFocusLine(int lineNumber)
		{
			this.Context.FocusRequestedPosition.Should().Be(this.Context.Filter.Rows.IndexOfLineNumber(lineNumber));
		}

		[Then("the grid will show and focus the last record")]
		public void ThenTheGridWillShowAndFocusTheLastRecord()
		{
			this.Context.FocusRequestedPosition.Should().Be(this.Context.Filter.Rows.Count - 1);
		}

		private RowViewModel GetRow(int lineNumber)
		{
			RowList rows = this.Context.Filter.Rows;

			return rows.GetRow(rows.IndexOfLineNumber(lineNumber));
		}

		private void Click(int lineNumber, ModifierKeys modifiers)
		{
			this.Context.Filter.PressRow(GetRow(lineNumber), isRightButton: false, modifiers);
		}
	}
}
