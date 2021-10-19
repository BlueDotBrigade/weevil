namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_activeIndex}, ActiveLineNumber={_activeRecord.LineNumber}")]
	internal class LineNumberNavigator : ILineNumberNavigator
	{
		private readonly ActiveRecord _navigator;

		public LineNumberNavigator(ActiveRecord navigator)
		{
			_navigator = navigator;
		}

		public IRecord Find(int lineNumber)
		{
			return Find(lineNumber, RecordSearchType.ExactMatch);
		}

		public IRecord Find(int lineNumber, RecordSearchType searchType)
		{
			switch (searchType)
			{
				case RecordSearchType.ExactMatch:
					// TODO: refactor code... weird we don't get index here
					return _navigator.SetActiveLineNumber(lineNumber);

				case RecordSearchType.ClosestMatch:
					var index = _navigator.Records.IndexOfLineNumber(lineNumber, RecordSearchType.ClosestMatch);
					var closestLineNumber = _navigator.Records[index].LineNumber;
					return _navigator.SetActiveLineNumber(closestLineNumber);

				default:
					throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null);
			}
		}
	}
}
