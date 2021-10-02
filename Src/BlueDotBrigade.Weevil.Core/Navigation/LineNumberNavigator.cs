namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_activeIndex}, ActiveLineNumber={_activeRecord.LineNumber}")]
	internal class LineNumberNavigator : ILineNumberNavigator
	{
		private readonly RecordNavigator _navigator;

		public LineNumberNavigator(RecordNavigator navigator)
		{
			_navigator = navigator;
		}

		public IRecord Find(int lineNumber)
		{
			return Find(lineNumber, SearchType.ExactMatch);
		}

		public IRecord Find(int lineNumber, SearchType searchType)
		{
			switch (searchType)
			{
				case SearchType.ExactMatch:
					// TODO: refactor code... weird we don't get index here
					return _navigator.SetActiveLineNumber(lineNumber);

				case SearchType.ClosestMatch:
					var index = _navigator.Records.IndexOfLineNumber(lineNumber, SearchType.ClosestMatch);
					var closestLineNumber = _navigator.Records[index].LineNumber;
					return _navigator.SetActiveLineNumber(closestLineNumber);

				default:
					throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null);
			}
		}
	}
}
