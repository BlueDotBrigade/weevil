namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	internal interface ILineNumberNavigator : INavigator
	{
		/// <summary>
		/// Performs a binary search looking for the <see cref="IRecord"/> that has the closest <paramref name="lineNumber"/>.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord Find(int lineNumber, RecordSearchType searchType);
	}
}
