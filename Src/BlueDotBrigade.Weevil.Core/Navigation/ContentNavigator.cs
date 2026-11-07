namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class ContentNavigator : IContentNavigator
	{
		private readonly ActiveRecord _activeRecord;

		public ContentNavigator(ActiveRecord activeRecord)
		{
			_activeRecord = activeRecord;
		}

		public IRecord FindPrevious(string value, bool isCaseSensitive, bool useRegex = false)
		{
			// Each row's content is read once, by position, into a buffer the search reuses (#1131),
			// so only the record found is created (#1150).
			RecordList records = _activeRecord.DataSource;
			var content = new ContentBuffer();

			if (useRegex)
			{
				var regexOptions = isCaseSensitive
					? RegexOptions.None
					: RegexOptions.IgnoreCase;
				var regex = new Regex(value, regexOptions);

				var resultAt = records.GoToPreviousPosition(_activeRecord.Index, position => regex.IsMatch(records.ReadContent(position, content)));

				return _activeRecord.SetActiveIndex(resultAt);
			}
			else
			{
				var comparison = isCaseSensitive
					? StringComparison.Ordinal
					: StringComparison.OrdinalIgnoreCase;

				var resultAt = records.GoToPreviousPosition(_activeRecord.Index, position => records.ReadContent(position, content).Contains(value, comparison));

				return _activeRecord.SetActiveIndex(resultAt);
			}
		}

		public IRecord FindNext(string value, bool isCaseSensitive, bool useRegex = false)
		{
			// Each row's content is read once, by position, into a buffer the search reuses (#1131),
			// so only the record found is created (#1150).
			RecordList records = _activeRecord.DataSource;
			var content = new ContentBuffer();

			if (useRegex)
			{
				var regexOptions = isCaseSensitive
					? RegexOptions.None
					: RegexOptions.IgnoreCase;
				var regex = new Regex(value, regexOptions);

				var resultAt = records.GoToNextPosition(_activeRecord.Index, position => regex.IsMatch(records.ReadContent(position, content)));

				return _activeRecord.SetActiveIndex(resultAt);
			}
			else
			{
				var comparison = isCaseSensitive
					? StringComparison.Ordinal
					: StringComparison.OrdinalIgnoreCase;

				var resultAt = records.GoToNextPosition(_activeRecord.Index, position => records.ReadContent(position, content).Contains(value, comparison));

				return _activeRecord.SetActiveIndex(resultAt);
			}
		}
	}
}
