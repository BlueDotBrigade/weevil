namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	internal interface ITimestampNavigator : INavigator
	{
		/// <summary>
		/// Performs a binary search looking for the <see cref="IRecord"/> that matches the provided <paramref name="timestamp"/>.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord Find(string timestamp, RecordSearchType searchType);
	}
}
