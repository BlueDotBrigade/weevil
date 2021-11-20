namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	internal interface IContentNavigator : INavigator
	{
		/// <summary>
		/// Search backwards through record <see cref="Record.Content"/> for the given text. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <exception cref="Record"/>
		IRecord FindPrevious(string value);

		/// <summary>
		/// Search forward through record <see cref="Record.Content"/> for the given text. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <exception cref="Record"/>
		IRecord FindNext(string value);
	}
}
