namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using Analysis;
	using BlueDotBrigade.Weevil.Collections;
	using BlueDotBrigade.Weevil.Diagnostics;
	using Data;
	using IO;
	using Navigation;

	/// <summary>
	/// The records selected in one view, stored as ranges of positions in its filter results (#1015).
	/// </summary>
	/// <remarks>
	/// A record that the filter hides is not selected: after each filter, the records it no longer shows leave the selection.
	/// Every change publishes a new <see cref="PositionRanges"/>, so readers never see one being changed.
	/// </remarks>
	[DebuggerDisplay("Selected={_selection.Count}")]
	internal class SelectionManager : ISelect
	{
		#region Fields
		private readonly object _selectionPadlock;

		private readonly Encoding _sourceFileEncoding;

		private readonly ImmutableArray<IRecord> _allRecords;
		private readonly Action _reapplyCurrentFilter;

		private readonly NavigationManager _navigationManager;

		private PositionRanges _selection;
		private ImmutableArray<IRecord> _visibleRecords;
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
			_selection = new PositionRanges();
			_selectionPadlock = new object();

			_sourceFileEncoding = sourceFileEncoding;

			_allRecords = allRecords;
			_visibleRecords = visibleRecords;
			_reapplyCurrentFilter = reapplyCurrentFilter;

			_navigationManager = navigationManager;

			_selectionPeriod = Metadata.ElapsedTimeUnknown;
		}
		#endregion

		#region Properties
		public IDictionary<int, IRecord> Selected
		{
			get
			{
				lock (_selectionPadlock)
				{
					return new SelectedRecords(_selection, _visibleRecords);
				}
			}
		}

		public bool HasSelectionPeriod => _selection.Count >= 2;

		public TimeSpan SelectionPeriod => _selectionPeriod;
		#endregion

		#region Static Members
		#endregion

		#region Private Methods

		private static TimeSpan CalculateTimePeriod(PositionRanges selection, ImmutableArray<IRecord> visibleRecords)
		{
			TimeSpan timePeriod = Metadata.ElapsedTimeUnknown;

			if (selection.Count > 0)
			{
				IRecord firstRecord = visibleRecords[selection.First];
				IRecord lastRecord = visibleRecords[selection.Last];

				if (firstRecord.HasCreationTime)
				{
					if (lastRecord.HasCreationTime)
					{
						timePeriod = lastRecord.CreatedAt - firstRecord.CreatedAt;
					}
				}
			}

			return timePeriod;
		}

		/// <summary>
		/// Returns the record's position in the filter results, or -1 when the filter hides it.
		/// </summary>
		private static int FindPosition(ImmutableArray<IRecord> visibleRecords, IRecord record)
		{
			var position = PositionRanges.LowerBound(visibleRecords, record.LineNumber, 0);

			return position < visibleRecords.Length && visibleRecords[position].LineNumber == record.LineNumber
				? position
				: -1;
		}

		/// <summary>
		/// Adds or removes the positions of the given records, then publishes the new selection.
		/// </summary>
		/// <returns>How many records were added or removed, and how many are selected afterwards.</returns>
		/// <remarks>
		/// The records change the selection in one pass, not one at a time, which would cost the number of records times
		/// the number of ranges.
		/// </remarks>
		private (int Changed, int Count) Change(IEnumerable<IRecord> records, bool isSelected)
		{
			lock (_selectionPadlock)
			{
				var positions = new List<int>();

				foreach (IRecord record in records)
				{
					var position = FindPosition(_visibleRecords, record);
					if (position >= 0)
					{
						positions.Add(position);
					}
				}

				var changes = PositionRanges.FromPositions(positions);
				PositionRanges selection = isSelected
					? PositionRanges.Union(_selection, changes)
					: PositionRanges.Except(_selection, changes);
				var changed = System.Math.Abs(selection.Count - _selection.Count);

				_selection = selection;
				_selectionPeriod = CalculateTimePeriod(selection, _visibleRecords);

				return (changed, selection.Count);
			}
		}

		private static ImmutableArray<IRecord> GetRecords(PositionRanges selection, ImmutableArray<IRecord> visibleRecords)
		{
			var records = new IRecord[selection.Count];
			var index = 0;

			foreach (var position in selection.EnumeratePositions())
			{
				records[index++] = visibleRecords[position];
			}

			return ImmutableArray.Create(records);
		}
		#endregion

		#region Event Handlers
		#endregion

		/// <summary>
		/// Keeps the selected records that the new filter results still show, and drops the others (#1015).
		/// </summary>
		/// <remarks>
		/// Called after each filter, on the filter's thread. An aborted filter never gets here, so it keeps the selection.
		/// </remarks>
		internal void KeepRecordsShownBy(ImmutableArray<IRecord> visibleRecords)
		{
			int before;
			int after;

			lock (_selectionPadlock)
			{
				before = _selection.Count;

				_selection = before == 0
					? new PositionRanges()
					: PositionRanges.Remap(_selection, _visibleRecords, visibleRecords, _allRecords);
				_visibleRecords = visibleRecords;
				_selectionPeriod = CalculateTimePeriod(_selection, _visibleRecords);

				after = _selection.Count;
			}

			if (before != after)
			{
				Log.Default.Write(
					LogSeverityType.Trace,
					$"The number of selected records has changed, because the filter hides some of them. Previous={before}, Current={after}");
			}
		}

		public ISelect Select(int lineNumber)
		{
			// One read: a filter may replace the results meanwhile.
			ImmutableArray<IRecord> visibleRecords = _visibleRecords;
			var index = visibleRecords.IndexOfLineNumber(lineNumber);

			if (index >= 0)
			{
				Select(visibleRecords[index]);
			}

			return this;
		}

		/// <remarks>
		/// A record that the filter hides is not selected.
		/// </remarks>
		public ISelect Select(IRecord record)
		{
			if (record == null)
			{
				throw new ArgumentNullException(nameof(record));
			}

			(var added, var count) = Change(new[] { record }, isSelected: true);

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. Added={added}, Current={count}");

			// Always update the active line number to keep navigation in sync,
			// even if the record was already selected.
			_navigationManager.SetActiveLineNumber(record.LineNumber);

			return this;
		}

		/// <remarks>
		/// Records that the filter hides are not selected.
		/// </remarks>
		public ISelect Select(IList<IRecord> records)
		{
			if (records == null)
			{
				throw new ArgumentNullException(nameof(records));
			}

			(var added, var count) = Change(records, isSelected: true);

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. Added={added}, Current={count}");

			// Always update the active line number to keep navigation in sync,
			// even if the last record was already selected.
			IRecord lastRecord = records.Count > 0 ? records[^1] : Record.Dummy;
			if (!Record.IsDummyOrNull(lastRecord))
			{
				_navigationManager.SetActiveLineNumber(lastRecord.LineNumber);
			}

			return this;
		}

		public ISelect Unselect(int lineNumber)
		{
			// One read: a filter may replace the results meanwhile.
			ImmutableArray<IRecord> visibleRecords = _visibleRecords;
			var index = visibleRecords.IndexOfLineNumber(lineNumber);

			if (index >= 0)
			{
				Unselect(visibleRecords[index]);
			}

			return this;
		}

		public ISelect Unselect(IRecord record)
		{
			if (record == null)
			{
				throw new ArgumentNullException(nameof(record));
			}

			(var removed, var count) = Change(new[] { record }, isSelected: false);

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. Removed={removed}, Current={count}");

			return this;
		}

		public ISelect Unselect(IList<IRecord> records)
		{
			if (records == null)
			{
				throw new ArgumentNullException(nameof(records));
			}

			(var removed, var count) = Change(records, isSelected: false);

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. Removed={removed}, Current={count}");

			return this;
		}

		public ISelect SaveSelection(string destinationFilePath, FileFormatType fileFormatType)
		{
			ImmutableArray<IRecord> sortedRecords = _selection.Count > 1
				? GetSelected()
				: _allRecords.OrderBy(x => x.LineNumber).ToImmutableArray();

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
			ImmutableArray<IRecord> clearedRecords;

			lock (_selectionPadlock)
			{
				clearedRecords = GetRecords(_selection, _visibleRecords);
				_selection = new PositionRanges();
				_selectionPeriod = Metadata.ElapsedTimeUnknown;
			}

			Log.Default.Write(
				LogSeverityType.Information,
				$"The number of selected records has changed. Previous={clearedRecords.Length}, Current=0");

			return clearedRecords;
		}

		public ImmutableArray<IRecord> GetSelected()
		{
			ImmutableArray<IRecord> selectedRecords;

			lock (_selectionPadlock)
			{
				selectedRecords = GetRecords(_selection, _visibleRecords);
			}

			Log.Default.Write(
				LogSeverityType.Debug,
				$"The selected records have been requested. Selected={selectedRecords.Length}");

			return selectedRecords;
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
