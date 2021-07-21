namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Immutable;
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

		public int ActiveIndex => _navigator.ActiveIndex;

		public int SetActiveRecord(int lineNumber)
		{
			return _navigator.SetActiveRecord(lineNumber);
		}

		public void UpdateDataSource(ImmutableArray<IRecord> records)
		{
			_navigator.UpdateDataSource(records);
		}

		public int GoToPrevious(string value)
		{
			return _navigator.GoToPrevious(record => record.Content.Contains(value));
		}

		public int GoToNext(string value)
		{
			return _navigator.GoToNext(record => record.Content.Contains(value));
		}
	}
}
