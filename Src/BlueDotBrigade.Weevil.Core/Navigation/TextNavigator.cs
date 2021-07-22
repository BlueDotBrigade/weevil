namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_navigator.ActiveIndex}, LineNumber={_navigator.ActiveRecord.LineNumber}")]
	internal class TextNavigator : ITextNavigator
	{
		private readonly RecordNavigator _navigator;

		public TextNavigator(RecordNavigator navigator)
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
