namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class FlagNavigator : IFlagNavigator
	{
		private readonly ActiveRecord _activeRecord;
		private readonly IRecordState _state;

		public FlagNavigator(ActiveRecord activeRecord, IRecordState state)
		{
			_activeRecord = activeRecord;
			_state = state;
		}

		private bool CheckIsFlagged(IRecord record)
		{
			return _state.IsFlagged(record);
		}

		public IRecord FindPrevious()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPrevious(_activeRecord.Index, CheckIsFlagged);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNext(_activeRecord.Index, CheckIsFlagged);
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
