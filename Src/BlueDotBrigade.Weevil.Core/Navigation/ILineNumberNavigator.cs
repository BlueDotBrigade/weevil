namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	internal interface ILineNumberNavigator : INavigator
	{
		/// <summary>
		/// Search for the <see cref="IRecord"/> that matches the provided line number.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
		IRecord Find(int lineNumber, RecordSearchType searchType);
	}
}
