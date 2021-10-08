namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	internal class RecordNavigator
	{
		private const int UnknownIndex = -1;

		private ImmutableArray<IRecord> _records;

		private int _activeIndex;
		private IRecord _activeRecord;

		public RecordNavigator(IList<IRecord> records) : this (records.ToImmutableArray())
		{
			// nothing to do
		}

		public RecordNavigator(ImmutableArray<IRecord> records)
		{
			if (records.IsDefault)
			{
				throw new ArgumentException("Immutable array has not been instantiated. Hint: call `Create()` method.");
			}

			_records = records;
			_activeIndex = UnknownIndex;
			_activeRecord = Record.Dummy;
		}

		public int ActiveIndex => _activeIndex;
		public IRecord ActiveRecord => _activeRecord;

		public ImmutableArray<IRecord> Records => _records;

		public IRecord SetActiveLineNumber(int lineNumber)
		{
			var index = _records.IndexOfLineNumber(lineNumber);

			if (index >= 0)
			{
				_activeIndex = index;
				_activeRecord = _records[index];
			}
			else
			{
				_activeIndex = UnknownIndex;
				_activeRecord = Record.Dummy;

				throw new RecordNotFoundException(
					lineNumber,
					$"Unable to find the given line number. Value={lineNumber}");
			}

			return _activeRecord;
		}

		public void UpdateDataSource(ImmutableArray<IRecord> newRecordCollection)
		{
			if (newRecordCollection.IsDefault)
			{
				throw new ArgumentException("Immutable array has not been instantiated. Hint: call `Create()` method.");
			}

			if (newRecordCollection.Length == 0)
			{
				_activeIndex = UnknownIndex;
				_activeRecord = Record.Dummy;
				_records = newRecordCollection;
			}
			else
			{
				var previousLineNumber = _records[_activeIndex].LineNumber;

				_records = newRecordCollection;

				if (_records.TryGetIndexOf(previousLineNumber, out var index))
				{
					_activeIndex = index;
					_activeRecord = _records[index];
				}
				else
				{
					// The record of interest does not exist in the new filter results.
					_activeIndex = UnknownIndex;
					_activeRecord = Record.Dummy;
				}
			}
		}

		/// <summary>
		/// Navigates through records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns the index of the <see cref="Record"/> that matches the search criteria.
		/// </returns>
		/// <exception cref="RecordNotFoundException"/>
		internal IRecord GoToPrevious(Func<IRecord, bool> checkIfMatches)
		{
			if (_records.Length == 0)
			{
				return Record.Dummy;
			}
			else
			{
				var wasFound = false;
				var index = _activeIndex;

				for (var i = 0; i < _records.Length; i++)
				{
					index = index - 1 < 0 ? _records.Length - 1 : index - 1;

					if (checkIfMatches(_records[index]))
					{
						_activeIndex = index;
						_activeRecord = _records[index];
						wasFound = true;
						break;
					}
				}

				return wasFound
					? _activeRecord
					: throw new RecordNotFoundException(-1);
			}
		}

		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns the index of the <see cref="Record"/> that matches the search criteria.
		/// </returns>
		internal IRecord GoToNext(Func<IRecord, bool> checkIfMatches)
		{
			if (_records.Length == 0)
			{
				return Record.Dummy;
			}
			else
			{
				var index = _activeIndex;

				var wasFound = false;

				for (var i = 0; i < _records.Length; i++)
				{
					index = (index + 1) % _records.Length;

					if (checkIfMatches(_records[index]))
					{
						_activeIndex = index;
						_activeRecord = _records[index];
						wasFound = true;
						break;
					}
				}

				return wasFound
					? _activeRecord
					: throw new RecordNotFoundException(-1);
			}
		}
	}
}
