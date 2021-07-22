namespace BlueDotBrigade.Weevil.Navigation
{
	using Data;


	public interface IPinNavigator : INavigator
	{
		/// <summary>
		/// Navigates through pinned records in descending order (e.g. lines: 4, 3, 2 1).
		/// </summary>
		IRecord FindPrevious();

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 1, 2, 3, 4, etc.).
		/// </summary>
		IRecord FindNext();
	}
}