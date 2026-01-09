namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	internal interface ICommentNavigator : INavigator
	{
		/// <summary>
		/// Searches backwards through records looking for a record with a comment. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord FindPrevious();

		/// <summary>
		/// Searches forwards through records looking for a record with a comment. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/> 
		IRecord FindNext();

		/// <summary>
		/// Searches backwards through records looking for a comment containing the specified text. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord FindPrevious(string text, bool isCaseSensitive, bool useRegex = false);

		/// <summary>
		/// Searches forwards through records looking for a comment containing the specified text. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord FindNext(string text, bool isCaseSensitive, bool useRegex = false);
	}
}