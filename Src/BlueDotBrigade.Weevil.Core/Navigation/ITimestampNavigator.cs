namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	internal interface ITimestampNavigator : INavigator
	{
		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 1, 2, 3, 4, etc.) looking for the provided timestamp.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord Find(string value, RecordSearchType searchType);
	}
}
