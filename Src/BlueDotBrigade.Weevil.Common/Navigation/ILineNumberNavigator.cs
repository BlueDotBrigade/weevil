﻿namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;

	public interface ILineNumberNavigator : INavigator
	{
		/// <summary>
		/// Search for the <see cref="IRecord"/> that matches the provided line number.
		/// </summary>
		IRecord Find(int lineNumber);

		/// <summary>
		/// Search for the <see cref="IRecord"/> that matches the provided line number.
		/// </summary>
		IRecord Find(int lineNumber, RecordSearchType searchType);
	}
}
