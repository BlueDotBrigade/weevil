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

		private bool CheckElapsedTime(IRecord record, int? minMilliseconds, int? maxMilliseconds)
		{
			if (!record.Metadata.HasElapsedTime)
			{
				return false;
			}

			var elapsedMs = record.Metadata.ElapsedTime.TotalMilliseconds;

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
