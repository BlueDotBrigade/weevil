namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Windows.Automation.Peers;
	using System.Windows.Automation.Provider;
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.TestTools.Data;

	/// <remarks>
	/// WPF gives a UI Automation client nothing for a grid outside a window, so these tests call the peers directly.
	/// </remarks>
	[TestClass]
	public class RecordListViewAutomationPeerTests
	{
		[STATestMethod]
		public void GivenASelectedRecord_WhenAScreenReaderAsksForTheSelection_ThenItGetsThatRow()
		{
			RecordListViewAutomationPeer grid = CreateGridPeer(selectedLineNumber: 2);

			List<RecordItemAutomationPeer> selectedRows = grid.GetSelectedRowPeers().ToList();
			RecordItemAutomationPeer firstSelectedRow = grid.FindNextSelectedRowPeer(startAfter: null);

			((ISelectionProvider)grid.GetPattern(PatternInterface.Selection)).CanSelectMultiple.Should().BeTrue();
			selectedRows.Select(row => row.LineNumber).Should().Equal(2);
			SelectionItem(selectedRows[0]).IsSelected.Should().BeTrue();
			firstSelectedRow.LineNumber.Should().Be(2);
			grid.FindNextSelectedRowPeer(firstSelectedRow).Should().BeNull();
		}

		[STATestMethod]
		public void GivenAnUnselectedRow_WhenAScreenReaderAddsItToTheSelection_ThenTheGridReportsACtrlClick()
		{
			RecordListViewAutomationPeer grid = CreateGridPeer(selectedLineNumber: 2);
			var firstRow = new RecordItemAutomationPeer(grid.Rows.GetRow(0), grid);
			RowPressedEventArgs pressed = null;
			((RecordListView)grid.Owner).RowPressed += (_, e) => pressed = e;

			SelectionItem(firstRow).AddToSelection();

			((RowViewModel)pressed.Item).LineNumber.Should().Be(1);
			pressed.Modifiers.Should().Be(ModifierKeys.Control);
		}

		[STATestMethod]
		[WorkItem(1077)]
		public void GivenASelectedRowOffScreen_WhenItsRowIsCollected_ThenAScreenReaderStillSeesItSelected()
		{
			// WPF holds a peer's row weakly, so the peer reported "not selected" once the garbage collector had run.
			RecordListViewAutomationPeer grid = CreateGridPeer(selectedLineNumber: 2);
			RecordItemAutomationPeer selectedRow = grid.GetSelectedRowPeers().Single();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			SelectionItem(selectedRow).IsSelected.Should().BeTrue();
		}

		private static RecordListViewAutomationPeer CreateGridPeer(int selectedLineNumber)
		{
			ImmutableArray<IRecord> results = R.WithLineNumbers(1, 3);
			var selector = Substitute.For<ISelect>();
			selector.Selected.Returns(new Dictionary<int, IRecord> { [selectedLineNumber] = results[selectedLineNumber - 1] });
			var grid = new RecordListView { ItemsSource = new RowList(results, new AnnotationStore(), selector, new UiDispatcherFake()) };

			return (RecordListViewAutomationPeer)UIElementAutomationPeer.CreatePeerForElement(grid);
		}

		private static ISelectionItemProvider SelectionItem(AutomationPeer row)
		{
			return (ISelectionItemProvider)row.GetPattern(PatternInterface.SelectionItem);
		}
	}
}
