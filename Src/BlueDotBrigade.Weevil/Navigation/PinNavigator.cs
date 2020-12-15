namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Immutable;
	using System.Diagnostics;
	using Data;
	using Weevil.Collections.Immutable;

	[DebuggerDisplay("ActiveIndex={_activeIndex}, ActiveLineNumber={_activeRecord.LineNumber}")]
	internal class PinNavigator : IPinNavigator
	{
		private const int IndexUnknown = -1;

		private ImmutableArray<IRecord> _filterResults;

		private IRecord _activeRecord;
		private int _activeIndex;

		public PinNavigator(ImmutableArray<IRecord> filterResults)
		{
			_filterResults = filterResults;

			_activeRecord = Record.Dummy;
			_activeIndex = IndexUnknown;
		}

		/// <summary>
		/// Represents the array index of the active record (think: highlighted).
		/// </summary>
		public int ActiveIndex => _activeIndex;

		internal void SetActiveRecord(int lineNumber)
		{
			var index = _filterResults.BinarySearch(new Record(lineNumber), new RecordLineNumberComparer());

			if (index >= 0)
			{
				_activeIndex = index;
				_activeRecord = _filterResults[index];
			}
			else
			{
				_activeRecord = Record.Dummy;
				_activeIndex = IndexUnknown;
			}
		}

		internal void UpdateDataSource(ImmutableArray<IRecord> newFilterResults)
		{
			if (newFilterResults.HasLineNumber(_activeRecord.LineNumber))
			{
				// nothing to do
				// ... we are pointing to a record that still exists
			}
			else
			{
				_activeRecord = Record.Dummy;
				_activeIndex = IndexUnknown;
			}

			_filterResults = newFilterResults;
		}

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public IRecord GoToPreviousPin()
		{
			var index = _activeIndex > _filterResults.Length ? 0 : _activeIndex;

			for (var i = 0; i < _filterResults.Length; i++)
			{
				index = index - 1 < 0 ? _filterResults.Length - 1 : index - 1;

				if (_filterResults[index].Metadata.IsPinned)
				{
					_activeRecord = _filterResults[index];
					_activeIndex = index;
					break;
				}
			}
			return _activeRecord;
		}

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public IRecord GoToNextPin()
		{
			var index = _activeIndex > _filterResults.Length ? 0 : _activeIndex;

			for (var i = 0; i < _filterResults.Length; i++)
			{
				index = (index + 1) % _filterResults.Length;

				if (_filterResults[index].Metadata.IsPinned)
				{
					_activeRecord = _filterResults[index];
					_activeIndex = index;
					break;
				}
			}

			return _activeRecord;
		}
	}
}
