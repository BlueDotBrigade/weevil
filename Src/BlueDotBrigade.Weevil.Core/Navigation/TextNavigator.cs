namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_navigator.Index}, LineNumber={_navigator.Record.LineNumber}")]
	internal class TextNavigator : ITextNavigator
	{
		private readonly ActiveRecord _navigator;

		public TextNavigator(ActiveRecord navigator)
		{
			_navigator = navigator;
		}

		public IRecord FindPrevious(string value)
		{
			return _navigator.GoToPrevious(record => record.Content.Contains(value));
		}

		public IRecord FindNext(string value)
		{
			return _navigator.GoToNext(record => record.Content.Contains(value));
		}
	}
}
