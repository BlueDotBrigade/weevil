namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	public interface ITextNavigator : INavigator
	{
		/// <summary>
		/// Navigates through records in descending order (e.g. lines: 4, 3, 2, 1) looking for the provided text.
		/// </summary>
		int GoToPrevious(string value);

		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 1, 2, 3, 4, etc.) looking for the provided text.
		/// </summary>
		int GoToNext(string value);
	}
}
