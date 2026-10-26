namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class ElapsedTimeNavigator : IElapsedTimeNavigator
	{
		private readonly ActiveRecord _activeRecord;

		// The view replaces its elapsed times after each filter.
		private readonly Func<ElapsedTimes> _getElapsedTimes;

		public ElapsedTimeNavigator(ActiveRecord activeRecord, Func<ElapsedTimes> getElapsedTimes)
		{
			_activeRecord = activeRecord;
			_getElapsedTimes = getElapsedTimes;
		}

		private bool CheckElapsedTime(IRecord record, int? minMilliseconds, int? maxMilliseconds)
		{
			TimeSpan elapsedTime = _getElapsedTimes().Get(record.LineNumber);

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
				.GoToPrevious(_activeRecord.Index, record => CheckElapsedTime(record, minMilliseconds, maxMilliseconds));
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext(int? minMilliseconds, int? maxMilliseconds)
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNext(_activeRecord.Index, record => CheckElapsedTime(record, minMilliseconds, maxMilliseconds));
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
