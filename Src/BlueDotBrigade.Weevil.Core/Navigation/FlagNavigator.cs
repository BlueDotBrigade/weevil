namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class FlagNavigator : IFlagNavigator
	{
		private readonly ActiveRecord _activeRecord;

		public FlagNavigator(ActiveRecord activeRecord)
		{
			_activeRecord = activeRecord;
		}

		private bool CheckIsFlagged(IRecord record)
		{
			return record.Metadata.IsFlagged;
		}

		/// <summary>
		/// Navigates through pinned records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public IRecord FindPrevious()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPrevious(_activeRecord.Index, CheckIsFlagged);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public IRecord FindNext()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNext(_activeRecord.Index, CheckIsFlagged);
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
