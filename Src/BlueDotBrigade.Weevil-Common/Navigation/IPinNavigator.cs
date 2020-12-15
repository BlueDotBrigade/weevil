namespace BlueDotBrigade.Weevil.Navigation
{
	using Data;

	public interface IPinNavigator
	{
		/// <summary>
		/// Represents the array index of the active record (think: highlighted).
		/// </summary>
		int ActiveIndex { get; }

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		IRecord GoToPreviousPin();

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		IRecord GoToNextPin();
	}
}