namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

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
		/// Raised when a mouse button is pressed on a row, outside its pin box and comment box.
		/// </summary>
		public event EventHandler<RowPressedEventArgs> RowPressed;

		public ListViewItem ContainerFromPosition(int position)
		{
			return this.ItemContainerGenerator.ContainerFromIndex(position) as ListViewItem;
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new RecordListViewItem();
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is RecordListViewItem;
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
			this.RowPressed?.Invoke(this, new RowPressedEventArgs(row, button));
		}
	}
}
