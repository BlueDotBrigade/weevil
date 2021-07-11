namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_activeIndex}, ActiveLineNumber={_activeRecord.LineNumber}")]
	internal class LineNumberNavigator : ILineNumberNavigator
	{
		private const int IndexUnknown = -1;

		private ImmutableArray<IRecord> _records;

		private IRecord _activeRecord;
		private int _activeIndex;

		public LineNumberNavigator(ImmutableArray<IRecord> records)
		{
			_records = records;

			_activeRecord = Record.Dummy;
			_activeIndex = IndexUnknown;
		}

		/// <summary>
		/// Represents the result of the the most recent navigation.
		/// </summary>
		public IRecord ActiveRecord => _activeRecord;

		/// <summary>
		/// Represents the result of the the most recent navigation.
		/// </summary>
		/// <returns>
		/// Returns the index value of the record for the latest filter results.
		/// </returns>
		public int ActiveIndex => _activeIndex;

		public IRecord GoToPrevious(int lineNumber)
		{
			SetActiveRecord(lineNumber);
			return _activeRecord;
		}

		public IRecord GoToPrevious(string lineNumber)
		{
			if (int.TryParse(lineNumber, out var line))
			{
				SetActiveRecord(line);
			}
			return _activeRecord;
		}

		public IRecord GoToNext(int lineNumber)
		{
			SetActiveRecord(lineNumber);
			return _activeRecord;
		}

		public IRecord GoToNext(string lineNumber)
		{
			if (int.TryParse(lineNumber, out var line))
			{
				SetActiveRecord(line);
			}

			return _activeRecord;
		}

		internal void SetActiveRecord(int lineNumber)
		{
			var index = _records.BinarySearch(new Record(lineNumber), new RecordLineNumberComparer());

			if (index >= 0)
			{
				_activeIndex = index;
				_activeRecord = _records[index];
			}
			else
			{
				_activeRecord = Record.Dummy;
				_activeIndex = IndexUnknown;
			}
		}

		internal void UpdateDataSource(ImmutableArray<IRecord> records)
		{
			if (records.HasLineNumber(_activeRecord.LineNumber))
			{
				// nothing to do
				// ... we are pointing to a record that still exists
			}
			else
			{
				_activeRecord = Record.Dummy;
				_activeIndex = IndexUnknown;
			}

			_records = records;
		}

		/// <summary>
		/// Navigates through records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns a reference to the previous <see cref="Record"/> that matches the search criteria.
		/// </returns>
		internal IRecord GoToPrevious(Func<IRecord, bool> getIsMatch)
		{
			var index = _activeIndex > _records.Length ? 0 : _activeIndex;

			for (var i = 0; i < _records.Length; i++)
			{
				index = index - 1 < 0 ? _records.Length - 1 : index - 1;

				if (getIsMatch(_records[index]))
				{
					_activeRecord = _records[index];
					_activeIndex = index;
					break;
				}
			}
			return _activeRecord;
		}

		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next <see cref="Record"/> that matches the search criteria.
		/// </returns>
		internal IRecord GoToNext(Func<IRecord, bool> getIsMatch)
		{
			var index = _activeIndex > _records.Length ? 0 : _activeIndex;

			for (var i = 0; i < _records.Length; i++)
			{
				index = (index + 1) % _records.Length;

				if (getIsMatch(_records[index]))
				{
					_activeRecord = _records[index];
					_activeIndex = index;
					break;
				}
			}

			return _activeRecord;
		}
	}
}
