namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class CommentNavigator : ICommentNavigator
	{
		private readonly ActiveRecord _activeRecord;
		private readonly RecordState _state;

		public CommentNavigator(ActiveRecord activeRecord, RecordState state)
		{
			_activeRecord = activeRecord;
			_state = state;
		}

		// By line number, so a search creates no record (#1140).
		private bool CheckHasComment(int lineNumber)
		{
			return _state.Annotations.HasComment(lineNumber);
		}

		public IRecord FindPrevious()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPreviousLineNumber(_activeRecord.Index, CheckHasComment);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNextLineNumber(_activeRecord.Index, CheckHasComment);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindPrevious(string value, bool isCaseSensitive, bool useRegex = false)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (useRegex)
			{
				var regexOptions = isCaseSensitive
					? RegexOptions.Compiled
					: RegexOptions.Compiled | RegexOptions.IgnoreCase;
				var regex = new Regex(value, regexOptions);

				var resultAt = _activeRecord
					.DataSource
					.GoToPreviousLineNumber(_activeRecord.Index, lineNumber => 
						_state.Annotations.HasComment(lineNumber) && regex.IsMatch(_state.Annotations.GetComment(lineNumber)));

				return _activeRecord.SetActiveIndex(resultAt);
			}
			else
			{
				var comparison = isCaseSensitive
					? StringComparison.Ordinal
					: StringComparison.OrdinalIgnoreCase;

				var resultAt = _activeRecord
					.DataSource
					.GoToPreviousLineNumber(_activeRecord.Index, lineNumber => 
						_state.Annotations.HasComment(lineNumber) && _state.Annotations.GetComment(lineNumber).Contains(value, comparison));

				return _activeRecord.SetActiveIndex(resultAt);
			}
		}

		public IRecord FindNext(string value, bool isCaseSensitive, bool useRegex = false)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (useRegex)
			{
				var regexOptions = isCaseSensitive
					? RegexOptions.Compiled
					: RegexOptions.Compiled | RegexOptions.IgnoreCase;
				var regex = new Regex(value, regexOptions);

				var resultAt = _activeRecord
					.DataSource
					.GoToNextLineNumber(_activeRecord.Index, lineNumber => 
						_state.Annotations.HasComment(lineNumber) && regex.IsMatch(_state.Annotations.GetComment(lineNumber)));

				return _activeRecord.SetActiveIndex(resultAt);
			}
			else
			{
				var comparison = isCaseSensitive
					? StringComparison.Ordinal
					: StringComparison.OrdinalIgnoreCase;

				var resultAt = _activeRecord
					.DataSource
					.GoToNextLineNumber(_activeRecord.Index, lineNumber => 
						_state.Annotations.HasComment(lineNumber) && _state.Annotations.GetComment(lineNumber).Contains(value, comparison));

				return _activeRecord.SetActiveIndex(resultAt);
			}
		}
	}
}
