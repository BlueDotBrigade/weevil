namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class ElapsedTimeNavigator : IElapsedTimeNavigator
	{
		private readonly ActiveRecord _activeRecord;

		// The view replaces its elapsed times after each filter, so they are read each time.
		private readonly RecordState _state;

		public ElapsedTimeNavigator(ActiveRecord activeRecord, RecordState state)
		{
			_activeRecord = activeRecord;
			_state = state;
		}

		// By line number, so a search creates no record (#1140).
		private bool CheckElapsedTime(int lineNumber, int? minMilliseconds, int? maxMilliseconds)
		{
			TimeSpan elapsedTime = _state.ElapsedTimes.Get(lineNumber);

			if (elapsedTime < TimeSpan.Zero)
			{
				return false;
			}

			var elapsedMs = elapsedTime.TotalMilliseconds;

			if (minMilliseconds.HasValue && elapsedMs < minMilliseconds.Value)
			{
				return false;
			}

			if (maxMilliseconds.HasValue && elapsedMs > maxMilliseconds.Value)
			{
				return false;
			}

			return true;
		}

		public IRecord FindPrevious(int? minMilliseconds, int? maxMilliseconds)
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPreviousLineNumber(_activeRecord.Index, lineNumber => CheckElapsedTime(lineNumber, minMilliseconds, maxMilliseconds));
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext(int? minMilliseconds, int? maxMilliseconds)
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNextLineNumber(_activeRecord.Index, lineNumber => CheckElapsedTime(lineNumber, minMilliseconds, maxMilliseconds));
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
