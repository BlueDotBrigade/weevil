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
		/// Search backwards for a record that was flagged by an analyzer. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		public IRecord FindPrevious()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPrevious(_activeRecord.Index, CheckIsFlagged);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		/// <summary>
		/// Search forward for a record that was flagged by an analzyer. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		public IRecord FindNext()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNext(_activeRecord.Index, CheckIsFlagged);
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
