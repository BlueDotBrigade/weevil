namespace BlueDotBrigade.Weevil.Navigation
{
	using Data;


	public interface ICommentNavigator : INavigator
	{
		/// <summary>
		/// Search backwards for a record with a comment.. Descending order: 4, 3, 2, 1.
		/// </summary>
		IRecord FindPrevious();

		/// <summary>
		/// Search forward for a record with a comment. Ascending order: 1, 2, 3, 4.
		/// </summary>
		IRecord FindNext();
	}
}