namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System;
	using System.Windows.Controls;
	using System.Windows.Input;

	internal sealed class RowPressedEventArgs : EventArgs
	{
		public RowPressedEventArgs(ListViewItem row, MouseButton button)
		{
			this.Row = row;
			this.Button = button;
		}

		public ListViewItem Row { get; }

		public MouseButton Button { get; }
	}
}
