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

		/// <summary>
		/// Search backwards for a record with a comment.. Descending order: 4, 3, 2, 1.
		/// </summary>
		public IRecord FindPrevious()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPrevious(_activeRecord.Index, CheckHasComment);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		/// <summary>
		/// Search forward for a record with a comment. Ascending order: 1, 2, 3, 4.
		/// </summary>
		public IRecord FindNext()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNext(_activeRecord.Index, CheckHasComment);
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
