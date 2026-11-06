namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class PinNavigator : IPinNavigator
	{
		private readonly ActiveRecord _activeRecord;
		private readonly RecordState _state;

		public PinNavigator(ActiveRecord activeRecord, RecordState state)
		{
			_activeRecord = activeRecord;
			_state = state;
		}

		// By line number, so a search creates no record (#1140).
		private bool CheckIsPinned(int lineNumber)
		{
			return _state.Annotations.IsPinned(lineNumber);
		}

		public IRecord FindPrevious()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPreviousLineNumber(_activeRecord.Index, CheckIsPinned);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNextLineNumber(_activeRecord.Index, CheckIsPinned);
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
