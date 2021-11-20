namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;

	internal interface IFlagNavigator : INavigator
	{
		/// <summary>
		/// Searches backwards for a record that was flagged by an analyzer. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		/// <exception cref="RecordNotFoundException"/>
		IRecord FindPrevious();

		/// <summary>
		/// Searches forwards for a record that was flagged by an analyzer. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		/// <exception cref="RecordNotFoundException"/>
		IRecord FindNext();
	}
}