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

		public CommentNavigator(ActiveRecord activeRecord)
		{
			_activeRecord = activeRecord;
		}

		private bool CheckHasComment(IRecord record)
		{
			return record.Metadata.HasComment;
		}

		public IRecord FindPrevious()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToPrevious(_activeRecord.Index, CheckHasComment);
			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext()
		{
			var resultAt = _activeRecord
				.DataSource
				.GoToNext(_activeRecord.Index, CheckHasComment);
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
					.GoToPrevious(_activeRecord.Index, record => 
						record.Metadata.HasComment && regex.IsMatch(record.Metadata.Comment));

				return _activeRecord.SetActiveIndex(resultAt);
			}
			else
			{
				var comparison = isCaseSensitive
					? StringComparison.Ordinal
					: StringComparison.OrdinalIgnoreCase;

				var resultAt = _activeRecord
					.DataSource
					.GoToPrevious(_activeRecord.Index, record => 
						record.Metadata.HasComment && record.Metadata.Comment.Contains(value, comparison));

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
					.GoToNext(_activeRecord.Index, record => 
						record.Metadata.HasComment && regex.IsMatch(record.Metadata.Comment));

				return _activeRecord.SetActiveIndex(resultAt);
			}
			else
			{
				var comparison = isCaseSensitive
					? StringComparison.Ordinal
					: StringComparison.OrdinalIgnoreCase;

				var resultAt = _activeRecord
					.DataSource
					.GoToNext(_activeRecord.Index, record => 
						record.Metadata.HasComment && record.Metadata.Comment.Contains(value, comparison));

				return _activeRecord.SetActiveIndex(resultAt);
			}
		}
	}
}
