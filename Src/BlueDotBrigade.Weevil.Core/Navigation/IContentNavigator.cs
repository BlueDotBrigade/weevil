namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	internal interface IContentNavigator : INavigator
	{
		/// <summary>
		/// Searches backwards through records looking for <see cref="Record.Content"/> with the provided text. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <exception cref="Record"/>
		IRecord FindPrevious(string value, bool isCaseSensitive, bool useRegex = false);

		/// <summary>
		/// Searches forward through records looking for <see cref="Record.Content"/> with the provided text. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <exception cref="Record"/>
		IRecord FindNext(string value, bool isCaseSensitive, bool useRegex = false);
	}
}
