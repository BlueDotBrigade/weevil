namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;

	public interface INavigate
	{
		ITableOfContents TableOfContents { get; }

		INavigate RebuildTableOfContents();

		/// <summary>
		/// Performs a binary search looking for the <see cref="IRecord"/> that has the closest <paramref name="lineNumber"/>.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord GoTo(int lineNumber, RecordSearchType recordSearchType);

		/// <summary>
		/// Performs a binary search looking for the <see cref="IRecord"/> that matches the provided <paramref name="timestamp"/>.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord GoTo(string timestamp, RecordSearchType recordSearchType);

		/// <summary>
		/// Searches backwards through records looking for <see cref="Record.Content"/> with the provided text. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <exception cref="Record"/>
		IRecord PreviousContent(string text);

		/// <summary>
		/// Searches forward through records looking for <see cref="Record.Content"/> with the provided text. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <exception cref="Record"/>
		IRecord NextContent(string text);

		/// <summary>
		/// Searches backwards through records looking for a pinned record. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <see cref="RecordNotFoundException"/>
		IRecord PreviousPin();

		/// <summary>
		/// Searches forwards through records looking for a pinned record. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <see cref="RecordNotFoundException"/>
		IRecord NextPin();

		/// <summary>
		/// Searches backwards through records looking for a record with a comment. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord PreviousComment();

		/// <summary>
		/// Searches forwards through records looking for a record with a comment. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/> 
		IRecord NextComment();

		/// <summary>
		/// Searches backwards for a record that was flagged by an analyzer. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		/// <exception cref="RecordNotFoundException"/>
		IRecord PreviousFlag();

		/// <summary>
		/// Search forward for a record that was flagged by an analyzer. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		/// <exception cref="RecordNotFoundException"/>
		IRecord NextFlag();
	}
}