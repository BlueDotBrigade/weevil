namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class CommentNavigator : ICommentNavigator
	{
		private readonly ActiveRecord _activeRecord;

		public CommentNavigator(ActiveRecord activeRecord)
		{
			_activeRecord = activeRecord;
		}

		private bool CheckHasComment(IRecord record)
		{
			return record.Metadata.HasComment;
		}

		public IRecord FindPrevious()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPrevious(_activeRecord.Index, CheckHasComment);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNext(_activeRecord.Index, CheckHasComment);
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
