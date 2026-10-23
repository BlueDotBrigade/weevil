namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System.Windows.Controls;
	using System.Windows.Input;

	/// <summary>
	/// A row of <see cref="RecordListView"/>: it reports presses instead of letting WPF select it (#1064).
	/// </summary>
	internal sealed class RecordListViewItem : ListViewItem
	{
		// The pin box and the comment box have already handled their own presses, so those do not reach the row.
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			Press(e, MouseButton.Left);
			base.OnMouseLeftButtonDown(e);
		}

		/// <remarks>
		/// The context menu opens on the button's release, after the row is selected.
		/// </remarks>
		protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
		{
			Press(e, MouseButton.Right);
			base.OnMouseRightButtonDown(e);
		}

		private void Press(MouseButtonEventArgs e, MouseButton button)
		{
			if (!e.Handled)
			{
				// Handled, so WPF neither selects the row nor captures the mouse.
				e.Handled = true;
				Focus();

				(ItemsControl.ItemsControlFromItemContainer(this) as RecordListView)?.OnRowPressed(this, button);
			}
		}
	}
}
