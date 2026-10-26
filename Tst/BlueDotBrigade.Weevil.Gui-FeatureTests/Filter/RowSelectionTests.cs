namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Gui.Threading;

	[TestClass]
	public class RowSelectionTests
	{
		private string _logPath = string.Empty;
		private IEngine _engine;
		private RowList _rows;
		private RowSelection _selection;

		[TestInitialize]
		public void Setup()
		{
			// One record per line: line N is at position N - 1.
			_logPath = Path.Combine(Path.GetTempPath(), $"RowSelectionTests.{Guid.NewGuid():N}.log");
			File.WriteAllLines(_logPath, Enumerable.Range(1, 50).Select(n => $"Record {n}"));

			_engine = Engine.UsingPath(_logPath).Open();
			_rows = new RowList(_engine.Filter.Results, _engine.Filter.ElapsedTimes, _engine.Annotations, _engine.Selector, new UiDispatcherFake());
			_selection = new RowSelection(_engine);
		}

		[TestCleanup]
		public void Cleanup()
		{
			File.Delete(_logPath);
			File.Delete(_logPath + ".xml");
		}

		[TestMethod]
		public void GivenASelection_WhenAnotherRowIsClicked_ThenOnlyThatRowIsSelected()
		{
			Click(lineNumber: 2);

			Click(lineNumber: 5);

			SelectedLines().Should().Equal(5);
			_engine.Selector.AnchorLineNumber.Should().Be(5);
		}

		[TestMethod]
		public void GivenAnAnchor_WhenShiftClicked_ThenTheRowsFromTheAnchorAreSelected()
		{
			Click(lineNumber: 2);

			Click(lineNumber: 5, ModifierKeys.Shift);

			SelectedLines().Should().Equal(2, 3, 4, 5);
			_engine.Selector.AnchorLineNumber.Should().Be(2);
		}

		[TestMethod]
		public void GivenASelection_WhenCtrlClicked_ThenTheRowIsToggled()
		{
			Click(lineNumber: 2);

			Click(lineNumber: 4, ModifierKeys.Control);
			SelectedLines().Should().Equal(2, 4);

			Click(lineNumber: 2, ModifierKeys.Control);
			SelectedLines().Should().Equal(4);
		}

		[TestMethod]
		public void GivenASelection_WhenCtrlShiftClicked_ThenTheRowsFromTheAnchorAreAdded()
		{
			Click(lineNumber: 1);
			Click(lineNumber: 8, ModifierKeys.Control);

			Click(lineNumber: 6, ModifierKeys.Control | ModifierKeys.Shift);

			SelectedLines().Should().Equal(1, 6, 7, 8);
		}

		[TestMethod]
		public void GivenASelectedRange_WhenRightClicked_ThenOnlyAnUnselectedRowReplacesIt()
		{
			Click(lineNumber: 2);
			Click(lineNumber: 5, ModifierKeys.Shift);

			// The context menu then acts on what was clicked.
			_selection.RightClick(_rows, 2, ModifierKeys.None);
			SelectedLines().Should().Equal(2, 3, 4, 5);

			_selection.RightClick(_rows, 9, ModifierKeys.None);
			SelectedLines().Should().Equal(10);
		}

		[TestMethod]
		public void GivenACaret_WhenShiftDownAndShiftEndArePressed_ThenEveryRowFromTheAnchorIsSelected()
		{
			Click(lineNumber: 41);

			PressKey(Key.Down, ModifierKeys.Shift);
			SelectedLines().Should().Equal(41, 42);

			var scrollTo = PressKey(Key.End, ModifierKeys.Shift);
			SelectedLines().Should().Equal(Enumerable.Range(41, 10));
			scrollTo.Should().Be(49);
		}

		[TestMethod]
		public void GivenACaret_WhenCtrlDownIsPressed_ThenOnlyTheCaretMoves()
		{
			Click(lineNumber: 3);

			PressKey(Key.Down, ModifierKeys.Control);
			SelectedLines().Should().Equal(3);
			_selection.CaretLineNumber.Should().Be(4);

			PressKey(Key.Space, ModifierKeys.Control);
			SelectedLines().Should().Equal(3, 4);
		}

		[TestMethod]
		public void GivenASelection_WhenCtrlShiftDownIsPressed_ThenTheRowIsAdded()
		{
			// Decided 2026-10-23: Ctrl+Shift+arrow adds to the selection, as Weevil 2.12 did.
			Click(lineNumber: 1);
			Click(lineNumber: 5, ModifierKeys.Control);

			PressKey(Key.Down, ModifierKeys.Control | ModifierKeys.Shift);

			SelectedLines().Should().Equal(1, 5, 6);
		}

		[TestMethod]
		public void GivenRows_WhenCtrlAIsPressed_ThenEveryRowIsSelected()
		{
			Click(lineNumber: 3);

			PressKey(Key.A, ModifierKeys.Control);

			_engine.Selector.Selected.Count.Should().Be(50);
			_engine.Selector.AnchorLineNumber.Should().Be(3);
		}

		[TestMethod]
		public void GivenASelectedRow_WhenALetterIsPressed_ThenNothingHappens()
		{
			// Decided 2026-10-23: typing a letter in the grid does nothing.
			Click(lineNumber: 3);

			var isHandled = _selection.TryPressKey(_rows, Key.B, ModifierKeys.None, 10, isOnRow: true, out _);

			isHandled.Should().BeFalse();
			SelectedLines().Should().Equal(3);
		}

		[TestMethod]
		public void GivenACaretTheFilterHides_WhenDownIsPressed_ThenTheRowAfterTheNextOneShownIsSelected()
		{
			Click(lineNumber: 3);
			_engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(@"Record \d*[05]$"));
			_rows = new RowList(_engine.Filter.Results, _engine.Filter.ElapsedTimes, _engine.Annotations, _engine.Selector, new UiDispatcherFake());

			// The caret takes the place of line 5, the next one shown, so Down goes to line 10.
			PressKey(Key.Down);

			SelectedLines().Should().Equal(10);
		}

		[TestMethod]
		public void GivenASelection_WhenAltSpaceOrAltDownIsPressed_ThenTheKeyIsLeftAlone()
		{
			// Alt+Space opens the window menu, as it did in Weevil 2.12.
			Click(lineNumber: 2);
			Click(lineNumber: 6, ModifierKeys.Shift);

			_selection.TryPressKey(_rows, Key.Space, ModifierKeys.Alt, 10, isOnRow: true, out _).Should().BeFalse();
			_selection.TryPressKey(_rows, Key.Down, ModifierKeys.Alt, 10, isOnRow: true, out _).Should().BeFalse();

			SelectedLines().Should().Equal(2, 3, 4, 5, 6);
		}

		[TestMethod]
		public void GivenACaretAndNoAnchor_WhenShiftClicked_ThenTheRowsFromTheCaretAreSelected()
		{
			// The filter hides the anchor, then shows the caret's record again.
			Click(lineNumber: 3);
			Click(lineNumber: 5, ModifierKeys.Control);
			_engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Record 1"));
			_engine.Filter.Apply(FilterType.PlainText, FilterCriteria.None);
			_rows = new RowList(_engine.Filter.Results, _engine.Filter.ElapsedTimes, _engine.Annotations, _engine.Selector, new UiDispatcherFake());
			_engine.Selector.AnchorLineNumber.Should().Be(-1);

			Click(lineNumber: 8, ModifierKeys.Shift);

			SelectedLines().Should().Equal(5, 6, 7, 8);
		}

		[TestMethod]
		public void GivenAFoundRecord_WhenNavigatedTo_ThenOnlyItIsSelectedAndBecomesTheCaret()
		{
			Click(lineNumber: 2);
			Click(lineNumber: 5, ModifierKeys.Shift);

			_selection.SelectOnly(_rows, 19);

			SelectedLines().Should().Equal(20);
			_selection.CaretLineNumber.Should().Be(20);
		}

		private void Click(int lineNumber, ModifierKeys modifiers = ModifierKeys.None)
		{
			_selection.Click(_rows, _rows.IndexOfLineNumber(lineNumber), modifiers);
		}

		private int PressKey(Key key, ModifierKeys modifiers = ModifierKeys.None, int pageSize = 10)
		{
			_selection.TryPressKey(_rows, key, modifiers, pageSize, isOnRow: true, out var scrollToPosition).Should().BeTrue();
			return scrollToPosition;
		}

		private int[] SelectedLines()
		{
			return _engine.Selector.Selected.Keys.ToArray();
		}
	}
}
