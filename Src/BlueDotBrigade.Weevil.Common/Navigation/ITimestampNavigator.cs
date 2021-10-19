namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	public interface ITimestampNavigator : INavigator
	{
		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 1, 2, 3, 4, etc.) looking for the provided timestamp.
		/// </summary>
		IRecord Find(string value, RecordSearchType searchType);
	}
}
