namespace BlueDotBrigade.Weevil.Navigation
{
	using Data;


	public interface IPinNavigator : INavigator
	{
		/// <summary>
		/// Navigates through pinned records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns the index of the previously pinned <see cref="Record"/>.
		/// </returns>
		int GoToPrevious();

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns the index of the next pinned <see cref="Record"/>.
		/// </returns>
		int GoToNext();
	}
}