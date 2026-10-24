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
	/// WPF holds a peer's row weakly, and so does <see cref="RowList"/>, so a row off screen can be collected while a client still holds its peer.
	/// The peer therefore keeps the row's line number and name, and asks Core whether the line is selected (#1077).
	/// </remarks>
	internal sealed class RecordItemAutomationPeer : GridViewItemAutomationPeer, ISelectionItemProvider
	{
		private readonly string _name;

		public RecordItemAutomationPeer(RowViewModel row, RecordListViewAutomationPeer listPeer)
			: base(row, listPeer)
		{
			this.LineNumber = row.LineNumber;
			_name = row.ToString();
		}

		public int LineNumber { get; }

		bool ISelectionItemProvider.IsSelected => this.IsSelected;

		IRawElementProviderSimple ISelectionItemProvider.SelectionContainer => ProviderFromPeer(this.ItemsControlAutomationPeer);

		private RowList Rows => ((RecordListViewAutomationPeer)this.ItemsControlAutomationPeer).Rows;

		private bool IsSelected => this.Rows?.IsSelected(this.LineNumber) == true;

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

		protected override string GetNameCore()
		{
			var name = base.GetNameCore();

			return string.IsNullOrEmpty(name) ? _name : name;
		}

		private void Press(ModifierKeys modifiers)
		{
			var grid = (RecordListView)this.ItemsControlAutomationPeer.Owner;

			if (!grid.IsEnabled)
			{
				throw new ElementNotEnabledException();
			}

			RowList rows = this.Rows;
			var position = rows?.IndexOfLineNumber(this.LineNumber) ?? -1;

			// A filter has hidden the row since the client found it.
			if (position < 0)
			{
				throw new ElementNotAvailableException();
			}

			grid.OnRowPressed(rows.GetRow(position), modifiers);
		}
	}
}
