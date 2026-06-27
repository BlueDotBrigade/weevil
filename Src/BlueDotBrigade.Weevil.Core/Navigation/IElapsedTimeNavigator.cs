namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using BlueDotBrigade.Weevil.Data;

	internal interface IElapsedTimeNavigator : INavigator
	{
		/// <summary>
		/// Searches backwards through records looking for a record with an elapsed time matching the criteria. Descending order: 4, 3, 2, 1.
		/// </summary>
		/// <param name="minMilliseconds">Minimum elapsed time in milliseconds. Use null for no minimum.</param>
		/// <param name="maxMilliseconds">Maximum elapsed time in milliseconds. Use null for no maximum.</param>
		/// <exception cref="RecordNotFoundException"/>
		IRecord FindPrevious(int? minMilliseconds, int? maxMilliseconds);

		/// <summary>
		/// Searches forwards through records looking for a record with an elapsed time matching the criteria. Ascending order: 1, 2, 3, 4.
		/// </summary>
		/// <param name="minMilliseconds">Minimum elapsed time in milliseconds. Use null for no minimum.</param>
		/// <param name="maxMilliseconds">Maximum elapsed time in milliseconds. Use null for no maximum.</param>
		/// <exception cref="RecordNotFoundException"/>
		IRecord FindNext(int? minMilliseconds, int? maxMilliseconds);
	}
}
