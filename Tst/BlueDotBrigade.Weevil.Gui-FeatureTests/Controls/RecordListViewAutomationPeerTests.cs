namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Windows.Automation;
	using System.Windows.Automation.Peers;
	using System.Windows.Automation.Provider;
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class RecordListViewAutomationPeerTests
	{
		[STATestMethod]
		public void GivenASelectedRecord_WhenAScreenReaderAsksForTheSelection_ThenItGetsThatRow()
		{
			AutomationPeer grid = UIElementAutomationPeer.CreatePeerForElement(CreateGrid(selectedLineNumber: 2));
			var selection = (ISelectionProvider)grid.GetPattern(PatternInterface.Selection);
			var rows = (IItemContainerProvider)grid.GetPattern(PatternInterface.ItemContainer);

			IRawElementProviderSimple[] selectedRows = selection.GetSelection();
			IRawElementProviderSimple firstSelectedRow = rows.FindItemByProperty(null, SelectionItemPatternIdentifiers.IsSelectedProperty.Id, true);

			selection.CanSelectMultiple.Should().BeTrue();
			selectedRows.Should().ContainSingle();
			SelectionItem(selectedRows[0]).IsSelected.Should().BeTrue();
			SelectionItem(firstSelectedRow).IsSelected.Should().BeTrue();
		}

		[STATestMethod]
		public void GivenAnUnselectedRow_WhenAScreenReaderAddsItToTheSelection_ThenTheGridReportsACtrlClick()
		{
			RecordListView grid = CreateGrid(selectedLineNumber: 2);
			var rows = (IItemContainerProvider)UIElementAutomationPeer.CreatePeerForElement(grid).GetPattern(PatternInterface.ItemContainer);
			RowPressedEventArgs pressed = null;
			grid.RowPressed += (_, e) => pressed = e;

			SelectionItem(rows.FindItemByProperty(null, 0, null)).AddToSelection();

			((RowViewModel)pressed.Item).LineNumber.Should().Be(1);
			pressed.Modifiers.Should().Be(ModifierKeys.Control);
		}

		private static RecordListView CreateGrid(int selectedLineNumber)
		{
			ImmutableArray<IRecord> results = R.WithLineNumbers(1, 3);
			var selector = Substitute.For<ISelect>();
			selector.Selected.Returns(new Dictionary<int, IRecord> { [selectedLineNumber] = results[selectedLineNumber - 1] });

			return new RecordListView { ItemsSource = new RowList(results, new AnnotationStore(), selector, new UiDispatcherFake()) };
		}

		private static ISelectionItemProvider SelectionItem(IRawElementProviderSimple row)
		{
			return (ISelectionItemProvider)row.GetPatternProvider(SelectionItemPatternIdentifiers.Pattern.Id);
		}
	}
}
