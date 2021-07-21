namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Immutable;
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

		public int GoTo(int lineNumber)
		{
			return _navigator.SetActiveRecord(lineNumber);
		}
	}
}
