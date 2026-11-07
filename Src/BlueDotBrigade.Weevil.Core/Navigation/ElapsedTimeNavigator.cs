namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class ElapsedTimeNavigator : IElapsedTimeNavigator
	{
		private readonly ActiveRecord _activeRecord;

		public ElapsedTimeNavigator(ActiveRecord activeRecord)
		{
			_activeRecord = activeRecord;
		}

		private static bool CheckElapsedTime(TimeSpan elapsedTime, int? minMilliseconds, int? maxMilliseconds)
		{
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

		// Each elapsed time is measured in the records searched, by position, as the grid shows it. So a search creates no record (#1140),
		// and does not calculate the elapsed times of every line on the UI thread first (#1150).
		public IRecord FindPrevious(int? minMilliseconds, int? maxMilliseconds)
		{
			RecordList records = _activeRecord.DataSource;
			var resultAt = records.GoToPreviousPosition(
				_activeRecord.Index,
				position => CheckElapsedTime(ElapsedTimes.Measure(records, position), minMilliseconds, maxMilliseconds));
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext(int? minMilliseconds, int? maxMilliseconds)
		{
			RecordList records = _activeRecord.DataSource;
			var resultAt = records.GoToNextPosition(
				_activeRecord.Index,
				position => CheckElapsedTime(ElapsedTimes.Measure(records, position), minMilliseconds, maxMilliseconds));
			return _activeRecord.SetActiveIndex(resultAt);
		}
	}
}
