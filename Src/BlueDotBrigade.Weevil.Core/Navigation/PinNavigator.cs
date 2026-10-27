namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class PinNavigator : IPinNavigator
	{
		private readonly ActiveRecord _activeRecord;
		private readonly IRecordState _state;

		public PinNavigator(ActiveRecord activeRecord, IRecordState state)
		{
			_activeRecord = activeRecord;
			_state = state;
		}

		private bool CheckIsPinned(IRecord record)
		{
			return _state.IsPinned(record);
		}

		public IRecord FindPrevious()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPrevious(_activeRecord.Index, CheckIsPinned);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNext(_activeRecord.Index, CheckIsPinned);
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
