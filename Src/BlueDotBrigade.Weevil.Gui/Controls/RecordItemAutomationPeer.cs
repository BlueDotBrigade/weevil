namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System.Windows.Automation;
	using System.Windows.Automation.Peers;
	using System.Windows.Automation.Provider;
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Gui.Filter;

	/// <summary>
	/// A row as UI Automation clients, such as screen readers, see it: selected when Core has selected its record (#1075).
	/// </summary>
	/// <remarks>
	/// A client changes the selection as a click would, so the grid's selection rules apply.
	/// </remarks>
	internal sealed class RecordItemAutomationPeer : GridViewItemAutomationPeer, ISelectionItemProvider
	{
		public RecordItemAutomationPeer(object item, RecordListViewAutomationPeer listPeer)
			: base(item, listPeer)
		{
		}

		bool ISelectionItemProvider.IsSelected => this.IsSelected;

		IRawElementProviderSimple ISelectionItemProvider.SelectionContainer => ProviderFromPeer(this.ItemsControlAutomationPeer);

		private bool IsSelected => this.Item is RowViewModel { IsSelected: true };

		void ISelectionItemProvider.Select()
		{
			Press(ModifierKeys.None);
		}

		void ISelectionItemProvider.AddToSelection()
		{
			if (!this.IsSelected)
			{
				Press(ModifierKeys.Control);
			}
		}

		void ISelectionItemProvider.RemoveFromSelection()
		{
			if (this.IsSelected)
			{
				Press(ModifierKeys.Control);
			}
		}

		private void Press(ModifierKeys modifiers)
		{
			var grid = (RecordListView)this.ItemsControlAutomationPeer.Owner;

			if (!grid.IsEnabled)
			{
				throw new ElementNotEnabledException();
			}

			grid.OnRowPressed(this.Item, modifiers);
		}
	}
}
