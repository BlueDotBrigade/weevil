namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Immutable;
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_navigator.ActiveIndex}, LineNumber={_navigator.ActiveRecord.LineNumber}")]
	internal class FindNavigator : IFindNavigator
	{
		private readonly GenericNavigator _navigator;

		public FindNavigator(ImmutableArray<IRecord> filterResults)
		{
			_navigator = new GenericNavigator(filterResults);
		}

		/// <summary>
		/// Represents the result of the the most recent navigation.
		/// </summary>
		/// <returns>
		/// Returns the index value of the record for the latest filter results.
		/// </returns>
		public int ActiveIndex => _navigator.ActiveIndex;

		internal void SetActiveRecord(int lineNumber)
		{
			_navigator.SetActiveRecord(lineNumber);
		}

		internal void UpdateDataSource(ImmutableArray<IRecord> newFilterResults)
		{
			_navigator.UpdateDataSource(newFilterResults);
		}

		/// <summary>
		/// Navigates through pinned records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public IRecord GoToPrevious(string value)
		{
			return _navigator.GoToPrevious(record => record.Content.Contains(value));
		}

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public IRecord GoToNext(string value)
		{
			return _navigator.GoToNext(record => record.Content.Contains(value));
		}
	}
}
