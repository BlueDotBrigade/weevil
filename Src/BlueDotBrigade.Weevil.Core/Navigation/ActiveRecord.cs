namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	internal class ActiveRecord
	{
		private const int UnknownIndex = -1;

		private ImmutableArray<IRecord> _activeRecords;

		private int _activeIndex;
		private IRecord _activeRecord;

		public ActiveRecord(IList<IRecord> records) : this (records.ToImmutableArray())
		{
			// nothing to do
		}

		public ActiveRecord(ImmutableArray<IRecord> records)
		{
			if (records.IsDefault)
			{
				throw new ArgumentException(
					"Immutable array should be initialized - consider calling Create() method.", nameof(records));
			}

			_activeRecords = records;
			_activeIndex = UnknownIndex;
			_activeRecord = Data.Record.Dummy;
		}

		public int Index => _activeIndex;
		public IRecord Record => _activeRecord;

		public ImmutableArray<IRecord> Records => _activeRecords;

		public IRecord SetActiveLineNumber(int lineNumber)
		{
			var index = _activeRecords.IndexOfLineNumber(lineNumber);

			if (index >= 0)
			{
				_activeIndex = index;
				_activeRecord = _activeRecords[index];
			}
			else
			{
				_activeIndex = UnknownIndex;
				_activeRecord = Data.Record.Dummy;

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
				throw new ArgumentException(
					"Immutable array should be initialized - consider calling Create() method.", nameof(newRecordCollection));
			}

			// Is collection empty?
			if (newRecordCollection.Length == 0)
			{
				_activeIndex = UnknownIndex;
				_activeRecord = Data.Record.Dummy;
				_activeRecords = newRecordCollection;
			}
			else if (_activeIndex == UnknownIndex)
			{
				// there is nothing to "restore"
				// ... only thing we can do is keep a reference to the new collection
				_activeIndex = UnknownIndex;
				_activeRecord = Data.Record.Dummy;
				_activeRecords = newRecordCollection;
			}
			else
			{
				var previousLineNumber = _activeRecords[_activeIndex].LineNumber;

				// Try to find the "record of interest" in the new collection
				if (_activeRecords.TryGetIndexOf(previousLineNumber, out var index))
				{
					_activeRecords = newRecordCollection;
					_activeIndex = index;
					_activeRecord = _activeRecords[index];
				}
				else
				{
					_activeRecords = newRecordCollection;
					_activeIndex = UnknownIndex;
					_activeRecord = Data.Record.Dummy;
				}
			}
		}

		/// <summary>
		/// Navigates through records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <returns>
		/// Returns the index of the <see cref="Data.Record"/> that matches the search criteria.
		/// </returns>
		/// <exception cref="RecordNotFoundException"/>
		internal IRecord GoToPrevious(Func<IRecord, bool> checkIfMatches)
		{
			if (_activeRecords.Length == 0)
			{
				return Data.Record.Dummy;
			}
			else
			{
				var wasFound = false;
				var index = _activeIndex;

				for (var i = 0; i < _activeRecords.Length; i++)
				{
					index = index - 1 < 0 ? _activeRecords.Length - 1 : index - 1;

					if (checkIfMatches(_activeRecords[index]))
					{
						_activeIndex = index;
						_activeRecord = _activeRecords[index];
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
		/// Returns the index of the <see cref="Data.Record"/> that matches the search criteria.
		/// </returns>
		internal IRecord GoToNext(Func<IRecord, bool> checkIfMatches)
		{
			if (_activeRecords.Length == 0)
			{
				return Data.Record.Dummy;
			}
			else
			{
				var index = _activeIndex;

				var wasFound = false;

				for (var i = 0; i < _activeRecords.Length; i++)
				{
					index = (index + 1) % _activeRecords.Length;

					if (checkIfMatches(_activeRecords[index]))
					{
						_activeIndex = index;
						_activeRecord = _activeRecords[index];
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
