namespace BlueDotBrigade.Weevil.Navigation
{
	using Data;


	public interface IFlagNavigator : INavigator
	{
		/// <summary>
		/// Search backwards for a record that was flagged by an analyzer. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		IRecord FindPrevious();

		/// <summary>
		/// Search forward for a record that was flagged by an analzyer. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		IRecord FindNext();
	}
}