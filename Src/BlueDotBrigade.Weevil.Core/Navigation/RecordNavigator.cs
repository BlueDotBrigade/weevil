namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	internal class RecordNavigator
	{
		private const int Unknown = -1;

		private ImmutableArray<IRecord> _records;

		private int _activeIndex;
		private int _activeLineNumber;
		private IRecord _activeRecord;

		public RecordNavigator(IList<IRecord> records) : this (records.ToImmutableArray())
		{
			// nothing to do
		}

		public RecordNavigator(ImmutableArray<IRecord> records)
		{
			_records = records;

			_activeLineNumber = Unknown;
			_activeIndex = Unknown;
			_activeRecord = Record.Dummy;
		}

		public int ActiveIndex => _activeIndex;
		public IRecord ActiveRecord => _activeRecord;


		public IRecord SetActiveRecord(int lineNumber)
		{
			var index = _records.BinarySearch(new Record(lineNumber), new RecordLineNumberComparer());

			if (index >= 0)
			{
				_activeIndex = index;
				_activeRecord = _records[index];
				_activeLineNumber = lineNumber;
			}
			else
			{
				_activeIndex = Unknown;
				_activeRecord = Record.Dummy;
				_activeLineNumber = Unknown;
			}

			return _activeRecord;
		}

		public void UpdateDataSource(ImmutableArray<IRecord> records)
		{
			if (records.HasLineNumber(_activeLineNumber))
			{
				// nothing to do
				// ... we are pointing to a record that still exists
			}
			else
			{
				_activeIndex = Unknown;
				_activeRecord = Record.Dummy;
				_activeLineNumber = Unknown;
			}

			_records = records;
		}

		internal IRecord GoToFirstMatch(Func<IRecord, bool> checkIfMatches)
		{
			_activeIndex = Unknown;
			_activeLineNumber = Unknown;

			for (var index = 0; index < _records.Length; index++)
			{
				if (checkIfMatches(_records[index]))
				{
					_activeIndex = index;
					_activeRecord = _records[index];
					_activeLineNumber = _records[index].LineNumber;
					break;
				}
			}

			return _activeRecord;
		}

		/// <summary>
		/// Navigates through records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns the index of the <see cref="Record"/> that matches the search criteria.
		/// </returns>
		internal IRecord GoToPrevious(Func<IRecord, bool> checkIfMatches)
		{
			var index = _activeIndex > _records.Length ? 0 : _activeIndex;

			for (var i = 0; i < _records.Length; i++)
			{
				index = index - 1 < 0 ? _records.Length - 1 : index - 1;

				if (checkIfMatches(_records[index]))
				{
					_activeIndex = index;
					_activeRecord = _records[index];
					_activeLineNumber = _records[index].LineNumber;
					break;
				}
			}
			return _activeRecord;
		}

		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns the index of the <see cref="Record"/> that matches the search criteria.
		/// </returns>
		internal IRecord GoToNext(Func<IRecord, bool> checkIfMatches)
		{
			var index = _activeIndex > _records.Length ? 0 : _activeIndex;

			for (var i = 0; i < _records.Length; i++)
			{
				index = (index + 1) % _records.Length;

				if (checkIfMatches(_records[index]))
				{
					_activeIndex = index;
					_activeRecord = _records[index];
					_activeLineNumber = _records[index].LineNumber;
					break;
				}
			}

			return _activeRecord;
		}
	}
}
