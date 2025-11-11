namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using Analysis;
	using BlueDotBrigade.Weevil.Diagnostics;
	using Data;
	using IO;
	using Navigation;

	[DebuggerDisplay("Selected={_selectedRecords.Count}")]
	internal class SelectionManager : ISelect
	{
		#region Fields
		private readonly IDictionary<int, IRecord> _selectedRecords;
		private readonly object _selectedRecordsPadlock;

		private readonly Encoding _sourceFileEncoding;

		private readonly ImmutableArray<IRecord> _allRecords;
		private readonly ImmutableArray<IRecord> _visibleRecords;
		private readonly Action _reapplyCurrentFilter;

		private readonly NavigationManager _navigationManager;

		private TimeSpan _selectionPeriod;
		#endregion

		#region Object Lifetime
		public SelectionManager(
			ImmutableArray<IRecord> allRecords,
			ImmutableArray<IRecord> visibleRecords,
			NavigationManager navigationManager,
			Encoding sourceFileEncoding,
			Action reapplyCurrentFilter)
		{
			_selectedRecords = new SortedDictionary<int, IRecord>();
			_selectedRecordsPadlock = new object();

			_sourceFileEncoding = sourceFileEncoding;

			_allRecords = allRecords;
			_visibleRecords = visibleRecords;
			_reapplyCurrentFilter = reapplyCurrentFilter;

			_navigationManager = navigationManager;

			_selectionPeriod = Metadata.ElapsedTimeUnknown;
		}
		#endregion

		#region Properties
		public IDictionary<int, IRecord> Selected => _selectedRecords;

		public bool HasSelectionPeriod => _selectedRecords.Count >= 2;

		public TimeSpan SelectionPeriod => _selectionPeriod;
		#endregion

		#region Static Members
		#endregion

		#region Private Methods

		private TimeSpan CalculateTimePeriod(IDictionary<int, IRecord> records)
		{
			TimeSpan timePeriod = Metadata.ElapsedTimeUnknown;

			lock (_selectedRecordsPadlock)
			{
				if (records.Count > 0)
				{
					IRecord firstRecord = records.Values.First();
					IRecord lastRecord = records.Values.Last();

					if (firstRecord.HasCreationTime)
					{
						if (lastRecord.HasCreationTime)
						{
							timePeriod = lastRecord.CreatedAt - firstRecord.CreatedAt;
						}
					}
				}
			}

			return timePeriod;
		}
		#endregion

		#region Event Handlers
		#endregion

		public ISelect Select(int lineNumber)
		{
			var index = _visibleRecords.IndexOfLineNumber(lineNumber);

			if (index >= 0)
			{
				Select(_visibleRecords[index]);
			}

			return this;
		}

		public ISelect Select(IRecord record)
		{
			if (record == null)
			{
				throw new ArgumentNullException(nameof(record));
			}

			IRecord firstRecord = Record.Dummy;

			var added = 0;
			var count = -1;

			lock (_selectedRecordsPadlock)
			{
				if (!_selectedRecords.ContainsKey(record.LineNumber))
				{
					added++;
					_selectedRecords.Add(record.LineNumber, record);
					firstRecord = record;
				}

				count = _selectedRecords.Count;
			}

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. Added={added}, Current={count}");

			_selectionPeriod = CalculateTimePeriod(_selectedRecords);

			if (!Record.IsDummyOrNull(firstRecord))
			{
				_navigationManager.SetActiveLineNumber(record.LineNumber);
			}

			return this;
		}

		public ISelect Select(IList<IRecord> records)
		{
			if (records == null)
			{
				throw new ArgumentNullException(nameof(records));
			}

			IRecord firstRecord = Record.Dummy;
			var added = 0;
			var count = -1;

			lock (_selectedRecordsPadlock)
			{
				foreach (IRecord record in records)
				{
					if (!_selectedRecords.ContainsKey(record.LineNumber))
					{
						added++;
						_selectedRecords.Add(record.LineNumber, record);

						if (Record.IsDummyOrNull(firstRecord))
						{
							firstRecord = record;
						}
					}
				}

				count = _selectedRecords.Count;
			}

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. Added={added}, Current={count}");

			if (!Record.IsDummyOrNull(firstRecord))
			{
				_navigationManager.SetActiveLineNumber(firstRecord.LineNumber);
			}

			_selectionPeriod = CalculateTimePeriod(_selectedRecords);

			return this;
		}

		public ISelect Unselect(int lineNumber)
		{
			var index = _visibleRecords.IndexOfLineNumber(lineNumber);

			if (index >= 0)
			{
				Unselect(_visibleRecords[index]);
			}

			return this;
		}

		public ISelect Unselect(IRecord record)
		{
			if (record == null)
			{
				throw new ArgumentNullException(nameof(record));
			}

			var removed = 0;
			var count = -1;

			lock (_selectedRecordsPadlock)
			{
				if (_selectedRecords.ContainsKey(record.LineNumber))
				{
					removed++;
					_selectedRecords.Remove(record.LineNumber);
				}

				count = _selectedRecords.Count;
			}

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. Removed={removed}, Current={count}");

			_selectionPeriod = CalculateTimePeriod(_selectedRecords);

			return this;
		}

		public ISelect Unselect(IList<IRecord> records)
		{
			if (records == null)
			{
				throw new ArgumentNullException(nameof(records));
			}

			var removed = 0;
			var count = -1;

			lock (_selectedRecordsPadlock)
			{
				foreach (IRecord record in records)
				{
					if (_selectedRecords.ContainsKey(record.LineNumber))
					{
						removed++;
						_selectedRecords.Remove(record.LineNumber);
					}
				}

				count = _selectedRecords.Count;
			}

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. Removed={removed}, Current={count}");

			_selectionPeriod = CalculateTimePeriod(_selectedRecords);

			return this;
		}

		public ISelect SaveSelection(string destinationFilePath, FileFormatType fileFormatType)
		{
			ImmutableArray<IRecord> sortedRecords;

			lock (_selectedRecordsPadlock)
			{
				if (_selectedRecords.Count > 1)
				{
					sortedRecords = _selectedRecords.Values.OrderBy(x => x.LineNumber).ToImmutableArray();
				}
				else
				{
					sortedRecords = _visibleRecords.OrderBy(x => x.LineNumber).ToImmutableArray();
				}
			}

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Selected records are being saved to disk...");

			if (sortedRecords != null)
			{
				new DiskWriter(destinationFilePath, _sourceFileEncoding, fileFormatType).Write(sortedRecords);

				Log.Default.Write(
					LogSeverityType.Trace,
					$"Selected records have been saved to disk. Count={sortedRecords.Length}");
			}

			return this;
		}

		public ImmutableArray<IRecord> ClearAll()
		{
			IRecord[] clearedRecords = null;

			lock (_selectedRecordsPadlock)
			{
				clearedRecords = _selectedRecords.Values.ToArray();

				_selectedRecords.Clear();
			}

			Log.Default.Write(
				LogSeverityType.Information,
				$"The number of selected records has changed. Previous={clearedRecords.Length}, Current=0");

			_selectionPeriod = CalculateTimePeriod(_selectedRecords);

			return ImmutableArray.Create(clearedRecords);
		}

		public ImmutableArray<IRecord> GetSelected()
		{
			IRecord[] selectedRecords = null;

			lock (_selectedRecordsPadlock)
			{
				if (_selectedRecords.Count > 1)
				{
					selectedRecords = _selectedRecords.Values.ToArray();
				}
				else
				{
					selectedRecords = _visibleRecords.ToArray();
				}
			}

			Log.Default.Write(
				LogSeverityType.Debug,
				$"The selected records have been requested. Selected={selectedRecords.Length}");

			return ImmutableArray.Create(selectedRecords);
		}

		public void ToggleIsPinned()
		{
			ImmutableArray<IRecord> selectedRecords = GetSelected();

			if (selectedRecords.Length > 0)
			{
				var isPinned = !selectedRecords[0].Metadata.IsPinned;
				foreach (IRecord record in selectedRecords)
				{
					record.Metadata.IsPinned = isPinned;
				}

				Log.Default.Write(
					LogSeverityType.Information,
					$"Changing record state. IsPinned={isPinned}, RecordsAffected={selectedRecords.Length}");
			}
		}
	}
}