namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class ContentNavigator : IContentNavigator
	{
		private readonly ActiveRecord _activeRecord;

		public ContentNavigator(ActiveRecord activeRecord)
		{
			_activeRecord = activeRecord;
		}

		public IRecord FindPrevious(string value, bool isCaseSensitive, bool useRegex = false, CancellationToken cancellationToken = default)
		{
			// Each row's content is read once, by position, into a buffer the search reuses (#1131),
			// so only the record found is created (#1150).
			// Also before the first record, so an empty list honours a search cancelled before it starts.
			cancellationToken.ThrowIfCancellationRequested();

			RecordList records = _activeRecord.DataSource;
			var content = new ContentBuffer();
			TextPredicate isMatch = CreatePredicate(value, isCaseSensitive, useRegex);

			var resultAt = records.GoToPreviousPosition(_activeRecord.Index, position => IsMatch(records, content, position, cancellationToken, isMatch));

			return _activeRecord.SetActiveIndex(resultAt);
		}

		public IRecord FindNext(string value, bool isCaseSensitive, bool useRegex = false, CancellationToken cancellationToken = default)
		{
			// Each row's content is read once, by position, into a buffer the search reuses (#1131),
			// so only the record found is created (#1150).
			// Also before the first record, so an empty list honours a search cancelled before it starts.
			cancellationToken.ThrowIfCancellationRequested();

			RecordList records = _activeRecord.DataSource;
			var content = new ContentBuffer();
			TextPredicate isMatch = CreatePredicate(value, isCaseSensitive, useRegex);

			var resultAt = records.GoToNextPosition(_activeRecord.Index, position => IsMatch(records, content, position, cancellationToken, isMatch));

			return _activeRecord.SetActiveIndex(resultAt);
		}

		private static TextPredicate CreatePredicate(string value, bool isCaseSensitive, bool useRegex)
		{
			if (useRegex)
			{
				var regex = new Regex(value, isCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

				return text => regex.IsMatch(text);
			}

			StringComparison comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

			return text => text.Contains(value, comparison);
		}

		/// <summary>
		/// Reads the row at <paramref name="position"/> and tests its text, unless the search was cancelled (#1027).
		/// </summary>
		/// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
		private static bool IsMatch(RecordList records, ContentBuffer content, int position, CancellationToken cancellationToken, TextPredicate isMatch)
		{
			cancellationToken.ThrowIfCancellationRequested();

			return isMatch(records.ReadContent(position, content));
		}

		private delegate bool TextPredicate(ReadOnlySpan<char> text);
	}
}
