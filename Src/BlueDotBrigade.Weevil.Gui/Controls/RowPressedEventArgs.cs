namespace BlueDotBrigade.Weevil.Gui.Controls
{
	using System;
	using System.Windows.Input;

	internal sealed class RowPressedEventArgs : EventArgs
	{
		public RowPressedEventArgs(object item, MouseButton button, ModifierKeys modifiers)
		{
			this.Item = item;
			this.Button = button;
			this.Modifiers = modifiers;
		}

		/// <summary>
		/// The item the row shows.
		/// </summary>
		public object Item { get; }

		public MouseButton Button { get; }

		public ModifierKeys Modifiers { get; }
	}
}
