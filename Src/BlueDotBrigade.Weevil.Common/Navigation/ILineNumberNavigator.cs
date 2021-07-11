namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	public interface ILineNumberNavigator
	{
		/// <summary>
		/// Represents the result of the the most recent navigation.
		/// </summary>
		/// <returns>
		/// Returns the index value of the record for the latest filter results.
		/// </returns>
		int ActiveIndex { get; }

		/// <summary>
		/// Navigates through records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		IRecord GoToPrevious(int lineNumber);

		/// <summary>
		/// Navigates through records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		IRecord GoToPrevious(string lineNumber);

		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		IRecord GoToNext(int lineNumber);

		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		IRecord GoToNext(string lineNumber);
	}
}
