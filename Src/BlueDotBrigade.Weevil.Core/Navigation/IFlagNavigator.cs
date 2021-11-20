namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;

	internal interface IFlagNavigator : INavigator
	{
		/// <summary>
		/// Search backwards for a record that was flagged by an analyzer. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		/// <exception cref="RecordNotFoundException"/>
		IRecord FindPrevious();

		/// <summary>
		/// Search forward for a record that was flagged by an analyzer. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <seealso cref="IRecordAnalyzer"/>
		/// <exception cref="RecordNotFoundException"/>
		IRecord FindNext();
	}
}