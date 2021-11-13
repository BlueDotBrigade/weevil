namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	internal class ActiveRecord
	{
		public const int UnknownIndex = -1;

		private ImmutableArray<IRecord> _dataSource;

		private int _activeIndex;
		private IRecord _activeRecord;

		public ActiveRecord(IList<IRecord> dataSource) : this (dataSource.ToImmutableArray())
		{
			// nothing to do
		}

		public ActiveRecord(ImmutableArray<IRecord> dataSource)
		{
			if (dataSource.IsDefault)
			{
				throw new ArgumentException(
					"Immutable array should be initialized - consider calling Create() method.", nameof(dataSource));
			}

			_dataSource = dataSource;
			_activeIndex = UnknownIndex;
			_activeRecord = Data.Record.Dummy;
		}

		public int Index => _activeIndex;
		public IRecord Record => _activeRecord;

		public ImmutableArray<IRecord> DataSource => _dataSource;

		/// <summary>
		/// Saves the <paramref name="index"/> of the record that is currently active.
		/// </summary>
		/// <param name="index">The index value to be saved. A value of <see cref="UnknownIndex"/> indicates that there is no active record.</param>
		/// <returns>Returns the record associated with the provided <paramref name="index"/>.</returns>
		/// <exception cref="RecordNotFoundException"/>
		public IRecord SetActiveIndex(int index)
		{
			// An active record has not been selected.
			if (index < 0)
			{
				_activeIndex = UnknownIndex;
				_activeRecord = Data.Record.Dummy;
			}
			else
			{
				// Update the active record.
				if (index < _dataSource.Length)
				{
					_activeIndex = index;
					_activeRecord = _dataSource[index];
				}
				// Index is out of range, throw an exception.
				else
				{
					_activeIndex = UnknownIndex;
					_activeRecord = Data.Record.Dummy;

					throw new RecordNotFoundException($"Unable to find record. Index={index}");
				}
			}

			return _activeRecord;
		}

		public void UpdateDataSource(ImmutableArray<IRecord> newRecordCollection)
		{
			if (newRecordCollection.IsDefault)
			{
				_activeIndex = UnknownIndex;
				_activeRecord = Data.Record.Dummy;
				_dataSource = ImmutableArray<IRecord>.Empty;

				throw new ArgumentException(
					"Immutable array should be initialized - consider calling Create() method.", nameof(newRecordCollection));
			}

			// Is collection empty?
			if (newRecordCollection.Length == 0)
			{
				_activeIndex = UnknownIndex;
				_activeRecord = Data.Record.Dummy;
				_dataSource = newRecordCollection;
			}
			else if (_activeIndex == UnknownIndex)
			{
				// there is nothing to "restore"
				// ... only thing we can do is keep a reference to the new collection
				_activeIndex = UnknownIndex;
				_activeRecord = Data.Record.Dummy;
				_dataSource = newRecordCollection;
			}
			else
			{
				var previousLineNumber = _dataSource[_activeIndex].LineNumber;

				// Try to find the "record of interest" in the new collection
				if (newRecordCollection.TryIndexOfLineNumber(previousLineNumber, out var newIndex))
				{
					_activeIndex = newIndex;
					_activeRecord = newRecordCollection[newIndex];
					_dataSource = newRecordCollection;
				}
				else
				{
					_activeIndex = UnknownIndex;
					_activeRecord = Data.Record.Dummy;
					_dataSource = newRecordCollection;
				}
			}
		}
	}
}
