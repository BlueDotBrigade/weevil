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

		IRecord GoTo(int lineNumber);

		IRecord GoTo(string lineNumber);
	}
}
