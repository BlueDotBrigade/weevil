namespace BlueDotBrigade.Weevil.Gui
{
	using System.Windows.Input;

	public class MenuItemViewModel
	{
		public MenuItemViewModel(string analyzerKey, string displayName, ICommand command)
		{
			this.DisplayText = displayName;
			this.Command = command;
			this.CommandParameter = new object[]
			{
				analyzerKey,
			};
		}

		public string DisplayText { get; set; }
		public ICommand Command { get; set; }
		public object[] CommandParameter { get; set; }
	}
}
