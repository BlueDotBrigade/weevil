namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	public interface IFindNavigator
	{
		/// <summary>
		/// Represents the result of the the most recent navigation.
		/// </summary>
		/// <returns>
		/// Returns the index value of the record for the latest filter results.
		/// </returns>
		int ActiveIndex { get; }

		/// <summary>
		/// Navigates through pinned records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		IRecord GoToPrevious(string value);

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		IRecord GoToNext(string value);
	}
}
