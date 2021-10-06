namespace BlueDotBrigade.Weevil.Navigation
{
	using Data;


	public interface IPinNavigator : INavigator
	{
		/// <summary>
		/// Search backwards for the previous pinned record. Descending order: 4, 3, 2, 1.
		/// </summary>
		IRecord FindPrevious();

		/// <summary>
		/// Search forward for the next pinned record. Ascending order: 1, 2, 3, 4.
		/// </summary>
		IRecord FindNext();
	}
}