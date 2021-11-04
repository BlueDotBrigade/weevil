namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class LineNumberNavigator : ILineNumberNavigator
	{
		private readonly ActiveRecord _activeRecord;

		public LineNumberNavigator(ActiveRecord activeRecord)
		{
			_activeRecord = activeRecord;
		}

		public IRecord Find(int lineNumber)
		{
			return Find(lineNumber, RecordSearchType.ExactMatch);
		}

		public IRecord Find(int lineNumber, RecordSearchType searchType)
		{
			var index = _activeRecord.DataSource.IndexOfLineNumber(lineNumber, searchType);
			return _activeRecord.SetActiveIndex(index);
		}
	}
}
