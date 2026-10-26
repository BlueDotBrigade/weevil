namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class PinNavigator : IPinNavigator
	{
		private readonly ActiveRecord _activeRecord;
		private readonly IAnnotations _annotations;

		public PinNavigator(ActiveRecord activeRecord, IAnnotations annotations)
		{
			_activeRecord = activeRecord;
			_annotations = annotations;
		}

		private bool CheckIsPinned(IRecord record)
		{
			return _annotations.IsPinned(record.LineNumber);
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
