namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class ContentNavigator : IContentNavigator
	{
		private readonly ActiveRecord _activeRecord;

		public ContentNavigator(ActiveRecord activeRecord)
		{
			_activeRecord = activeRecord;
		}

		public IRecord FindPrevious(string value)
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPrevious(_activeRecord.Index, record => record.Content.Contains(value));
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext(string value)
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNext(_activeRecord.Index, record => record.Content.Contains(value));
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
