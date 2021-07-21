namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Immutable;
	using System.Diagnostics;
	using Data;

	[DebuggerDisplay("ActiveIndex={_navigator.ActiveIndex}, LineNumber={_navigator.ActiveRecord.LineNumber}")]
	internal class PinNavigator : IPinNavigator
	{
		private readonly RecordNavigator _navigator;

		public PinNavigator(RecordNavigator navigator)
		{
			_navigator = navigator;
		}

		private bool CheckIsPinned(IRecord record)
		{
			return record.Metadata.IsPinned;
		}

		public int ActiveIndex => _navigator.ActiveIndex;

		public int SetActiveRecord(int lineNumber)
		{
			return _navigator.SetActiveRecord(lineNumber);
		}

		public void UpdateDataSource(ImmutableArray<IRecord> records)
		{
			_navigator.UpdateDataSource(records);
		}

		/// <summary>
		/// Navigates through pinned records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public int GoToPrevious()
		{
			return _navigator.GoToPrevious(CheckIsPinned);
		}

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public int GoToNext()
		{
			return _navigator.GoToNext(CheckIsPinned);
		}
	}
}
