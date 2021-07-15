namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	public interface ITimestampNavigator
	{
		/// <summary>
		/// Represents the result of the the most recent navigation.
		/// </summary>
		/// <returns>
		/// Returns the index value of the record for the latest filter results.
		/// </returns>
		int ActiveIndex { get; }

		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 2, 4, 8, 16) looking for the provided text.
		/// </summary>
		IRecord GoTo(string value);
	}
}
