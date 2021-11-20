namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	internal interface IPinNavigator : INavigator
	{
		/// <summary>
		/// Search backwards for the previous pinned record. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <see cref="RecordNotFoundException"/>
		IRecord FindPrevious();

		/// <summary>
		/// Search forward for the next pinned record. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <see cref="RecordNotFoundException"/>
		IRecord FindNext();
	}
}