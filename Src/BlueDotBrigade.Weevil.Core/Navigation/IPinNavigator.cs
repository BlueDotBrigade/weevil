namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	internal interface IPinNavigator : INavigator
	{
		/// <summary>
		/// Searches backwards through records looking for a pinned record. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <see cref="RecordNotFoundException"/>
		IRecord FindPrevious();

		/// <summary>
		/// Searches forwards through records looking for a pinned record.. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <see cref="RecordNotFoundException"/>
		IRecord FindNext();
	}
}