namespace BlueDotBrigade.Weevil.Navigation
{
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

		/// <summary>
		/// Navigates through pinned records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public IRecord FindPrevious()
		{
			return _navigator.GoToPrevious(CheckIsPinned);
		}

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public IRecord FindNext()
		{
			return _navigator.GoToNext(CheckIsPinned);
		}
	}
}
