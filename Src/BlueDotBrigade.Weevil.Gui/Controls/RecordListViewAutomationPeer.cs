namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Automation;
	using System.Windows.Automation.Peers;
	using System.Windows.Automation.Provider;
	using System.Windows.Controls;
	using BlueDotBrigade.Weevil.Gui.Filter;

	/// <summary>
	/// Tells UI Automation clients, such as screen readers, which rows Core has selected (#1075).
	/// </summary>
	/// <remarks>
	/// WPF's peer reports WPF's selection, which is always empty, because WPF never selects a row (#1064).
	/// WPF implements the selection interfaces explicitly, so this class implements them again, and UI Automation calls these instead.
	/// </remarks>
	internal sealed class RecordListViewAutomationPeer : ListViewAutomationPeer, ISelectionProvider
	{
		// A client asking for every selected row gets the first ones only: otherwise Ctrl+A would create a row for every record.
		private const int MaximumSelectionReported = 1_000;

		public RecordListViewAutomationPeer(RecordListView owner)
			: base(owner)
		{
			// As ListView does: the grid's peer reports its rows and columns.
			if (owner.View is GridView view)
			{
				this.ViewAutomationPeer = new GridViewAutomationPeer(view, owner);
			}
		}

		bool ISelectionProvider.CanSelectMultiple => true;

		bool ISelectionProvider.IsSelectionRequired => false;

		private RecordListView Grid => (RecordListView)this.Owner;

		private RowList Rows => this.Grid.ItemsSource as RowList;

		IRawElementProviderSimple[] ISelectionProvider.GetSelection()
		{
			RowList rows = this.Rows;

			if (rows == null)
			{
				return Array.Empty<IRawElementProviderSimple>();
			}

			return rows
				.GetSelectedPositions(afterPosition: -1)
				.Take(MaximumSelectionReported)
				.Select(position => ProviderFor(rows.GetRow(position)))
				.Where(provider => provider != null)
				.ToArray();
		}

		public override object GetPattern(PatternInterface patternInterface)
		{
			object pattern = base.GetPattern(patternInterface);

			return patternInterface == PatternInterface.ItemContainer && pattern is IItemContainerProvider wpfFinder
				? new SelectedItemFinder(this, wpfFinder)
				: pattern;
		}

		/// <summary>
		/// Raises the events WPF raises when it selects rows: rows on screen report their change, and the list reports the selection.
		/// </summary>
		/// <param name="changedRows">The rows still held whose selection changed.</param>
		internal void RaiseSelectionChanged(IReadOnlyList<RowViewModel> changedRows)
		{
			// Beyond this, WPF tells clients to read the selection again instead of reporting each row.
			var isInvalidated = changedRows.Count > AutomationInteropProvider.InvalidateLimit;
			var isOnlyRowSelected = this.Rows?.SelectedCount == 1;

			foreach (RowViewModel row in changedRows)
			{
				// Only a row on screen that a client has seen has a peer.
				if (this.Grid.ItemContainerGenerator.ContainerFromItem(row) is UIElement container &&
					UIElementAutomationPeer.FromElement(container)?.EventsSource is RecordItemAutomationPeer rowPeer)
				{
					rowPeer.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, !row.IsSelected, row.IsSelected);

					if (!isInvalidated)
					{
						rowPeer.RaiseAutomationEvent(
							!row.IsSelected ? AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection
							: isOnlyRowSelected ? AutomationEvents.SelectionItemPatternOnElementSelected
							: AutomationEvents.SelectionItemPatternOnElementAddedToSelection);
					}
				}
			}

			if (isInvalidated)
			{
				RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
			}
		}

		protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
		{
			return new RecordItemAutomationPeer(item, this);
		}

		private IRawElementProviderSimple ProviderFor(RowViewModel row)
		{
			ItemAutomationPeer peer = FindOrCreateItemAutomationPeer(row);

			return peer == null ? null : ProviderFromPeer(peer);
		}

		/// <summary>
		/// Finds the next selected row in Core's selection. WPF's search would create a row and a peer for every record.
		/// </summary>
		/// <remarks>
		/// It is a separate object: if the list's peer implemented the interface again, calling WPF's search through the interface would call itself.
		/// </remarks>
		private sealed class SelectedItemFinder : IItemContainerProvider
		{
			private readonly RecordListViewAutomationPeer _listPeer;
			private readonly IItemContainerProvider _wpfFinder;

			public SelectedItemFinder(RecordListViewAutomationPeer listPeer, IItemContainerProvider wpfFinder)
			{
				_listPeer = listPeer;
				_wpfFinder = wpfFinder;
			}

			public IRawElementProviderSimple FindItemByProperty(IRawElementProviderSimple startAfter, int propertyId, object value)
			{
				RowList rows = _listPeer.Rows;

				if (propertyId != SelectionItemPatternIdentifiers.IsSelectedProperty.Id || value is not true || rows == null)
				{
					return _wpfFinder.FindItemByProperty(startAfter, propertyId, value);
				}

				var afterPosition = -1;

				if (startAfter != null)
				{
					afterPosition = rows.IndexOf((_listPeer.PeerFromProvider(startAfter) as ItemAutomationPeer)?.Item);

					if (afterPosition < 0)
					{
						return null;
					}
				}

				foreach (var position in rows.GetSelectedPositions(afterPosition))
				{
					return _listPeer.ProviderFor(rows.GetRow(position));
				}

				return null;
			}
		}
	}
}
