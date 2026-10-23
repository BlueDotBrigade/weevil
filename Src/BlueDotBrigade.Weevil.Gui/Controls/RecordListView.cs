namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Automation.Peers;
	using System.Windows.Controls;
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Gui.Filter;

	/// <summary>
	/// A list of records that never selects a row itself: it reports presses, and the selection lives in Core (#1064).
	/// </summary>
	/// <remarks>
	/// In <see cref="SelectionMode.Multiple"/>, moving keyboard focus between rows does not select them, which
	/// <see cref="SelectionMode.Single"/> and <see cref="SelectionMode.Extended"/> do.
	/// Text search is off: it would compare every row with the letter typed.
	/// </remarks>
	internal sealed class RecordListView : ListView
	{
		public RecordListView()
		{
			this.SelectionMode = SelectionMode.Multiple;
			this.IsSynchronizedWithCurrentItem = false;
			this.IsTextSearchEnabled = false;
		}

		/// <summary>
		/// Raised when a mouse button is pressed on a row, outside its pin box and comment box,
		/// and when a UI Automation client, such as a screen reader, selects or unselects a row, as a click would.
		/// </summary>
		public event EventHandler<RowPressedEventArgs> RowPressed;

		public ListViewItem ContainerFromPosition(int position)
		{
			return this.ItemContainerGenerator.ContainerFromIndex(position) as ListViewItem;
		}

		/// <summary>
		/// Screen readers see Core's selection, because WPF's own is always empty (#1075).
		/// </summary>
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new RecordListViewAutomationPeer(this);
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new RecordListViewItem();
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is RecordListViewItem;
		}

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);

			if (oldValue is RowList oldRows)
			{
				oldRows.SelectionRefreshed -= OnSelectionRefreshed;
			}

			if (newValue is RowList newRows)
			{
				newRows.SelectionRefreshed += OnSelectionRefreshed;
			}
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);

			// A safety net: WPF selected a row by some path the rows do not handle.
			if (this.SelectedItems.Count > 0)
			{
				UnselectAll();
			}
		}

		internal void OnRowPressed(RecordListViewItem row, MouseButton button)
		{
			this.RowPressed?.Invoke(this, new RowPressedEventArgs(row.DataContext, button, Keyboard.Modifiers));
		}

		/// <summary>
		/// A UI Automation client selected or unselected the row that shows <paramref name="item"/>.
		/// </summary>
		internal void OnRowPressed(object item, ModifierKeys modifiers)
		{
			this.RowPressed?.Invoke(this, new RowPressedEventArgs(item, MouseButton.Left, modifiers));
		}

		private void OnSelectionRefreshed(object sender, IReadOnlyList<RowViewModel> changedRows)
		{
			// The peer exists only once a UI Automation client has asked for it.
			(UIElementAutomationPeer.FromElement(this) as RecordListViewAutomationPeer)?.RaiseSelectionChanged(changedRows);
		}
	}
}
