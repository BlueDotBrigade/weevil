namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using System.Runtime.InteropServices;
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
		private const int NoLineNumber = -1;

		#region Fields
		private readonly object _selectionPadlock;

		private readonly Encoding _sourceFileEncoding;

		private readonly RecordScope _scope;
		private readonly Action _reapplyCurrentFilter;

		private readonly NavigationManager _navigationManager;
		private readonly RecordState _state;

		private PositionRanges _selection;
		private RecordList _visibleRecords;
		private TimeSpan _selectionPeriod;
		private int _anchorLineNumber;
		#endregion

		#region Object Lifetime
		public SelectionManager(
			RecordScope scope,
			RecordList visibleRecords,
			NavigationManager navigationManager,
			Encoding sourceFileEncoding,
			RecordState state,
			Action reapplyCurrentFilter)
		{
			_state = state ?? throw new ArgumentNullException(nameof(state));

			_selection = new PositionRanges();
			_selectionPadlock = new object();

			_sourceFileEncoding = sourceFileEncoding;

			_scope = scope;
			_visibleRecords = visibleRecords;
			_reapplyCurrentFilter = reapplyCurrentFilter;

			_navigationManager = navigationManager;

			_selectionPeriod = ElapsedTimes.Unknown;
			_anchorLineNumber = NoLineNumber;
		}
		#endregion

		#region Properties
		public int SelectedCount
		{
			get
			{
				lock (_selectionPadlock)
				{
					return _selection.Count;
				}
			}
		}

		public bool IsSelected(int lineNumber)
		{
			PositionRanges selection;
			RecordList visibleRecords;

			lock (_selectionPadlock)
			{
				selection = _selection;
				visibleRecords = _visibleRecords;
			}

			// Both are immutable, so the search needs no lock.
			var position = FindPosition(visibleRecords, lineNumber);
			return position >= 0 && selection.Contains(position);
		}

		public bool HasSelectionPeriod => _selection.Count >= 2;

		public TimeSpan SelectionPeriod => _selectionPeriod;

		public int AnchorLineNumber => _anchorLineNumber;
		#endregion

		#region Static Members
		#endregion

		#region Private Methods

		private static TimeSpan CalculateTimePeriod(PositionRanges selection, RecordList visibleRecords)
		{
			TimeSpan timePeriod = ElapsedTimes.Unknown;

			if (selection.Count > 0)
			{
				DateTime firstCreatedAt = visibleRecords.GetCreatedAt(selection.First);
				DateTime lastCreatedAt = visibleRecords.GetCreatedAt(selection.Last);

				if (firstCreatedAt != Record.CreationTimeUnknown && lastCreatedAt != Record.CreationTimeUnknown)
				{
					timePeriod = lastCreatedAt - firstCreatedAt;
				}
			}

			return timePeriod;
		}

		/// <summary>
		/// Returns the record's position in the filter results, or -1 when the filter hides it.
		/// </summary>
		private static int FindPosition(RecordList visibleRecords, int lineNumber)
		{
			var position = PositionRanges.LowerBound(visibleRecords, lineNumber, 0);

			return position < visibleRecords.Count && visibleRecords.GetLineNumber(position) == lineNumber
				? position
				: -1;
		}

		/// <summary>
		/// Returns the selected records as a list over the results and the selected ranges, which copies neither (#1140).
		/// </summary>
		private static RecordList GetRecords(PositionRanges selection, RecordList visibleRecords)
		{
			return selection.Count == 0
				? RecordList.Empty
				: new SelectedRecordList(selection, visibleRecords);
		}

		/// <summary>
		/// Returns the line numbers of the selected positions, read without creating records (#1137).
		/// </summary>
		private static int[] GetLineNumbers(PositionRanges selection, RecordList visibleRecords)
		{
			var lineNumbers = new int[selection.Count];
			var index = 0;

			foreach (var position in selection.EnumeratePositions())
			{
				lineNumbers[index++] = visibleRecords.GetLineNumber(position);
			}

			return lineNumbers;
		}

		private int[] GetSelectedLineNumbers()
		{
			PositionRanges selection;
			RecordList visibleRecords;

			lock (_selectionPadlock)
			{
				selection = _selection;
				visibleRecords = _visibleRecords;
			}

			return GetLineNumbers(selection, visibleRecords);
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
		internal void KeepRecordsShownBy(RecordList visibleRecords)
		{
			int before;
			int after;

			lock (_selectionPadlock)
			{
				before = _selection.Count;

				_selection = before == 0
					? new PositionRanges()
					: PositionRanges.Remap(_selection, _visibleRecords, visibleRecords, _scope.Records);
				_visibleRecords = visibleRecords;
				_selectionPeriod = CalculateTimePeriod(_selection, _visibleRecords);

				if (FindPosition(_visibleRecords, _anchorLineNumber) < 0)
				{
					_anchorLineNumber = NoLineNumber;
				}

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
			return ChangeLine(lineNumber, isSelected: true);
		}

		public ISelect Unselect(int lineNumber)
		{
			return ChangeLine(lineNumber, isSelected: false);
		}

		/// <summary>
		/// Adds one shown line to the selection, or removes it, and makes an added line the active record.
		/// </summary>
		/// <exception cref="RecordNotFoundException">The filter hides the line. The selection does not change.</exception>
		private ISelect ChangeLine(int lineNumber, bool isSelected)
		{
			int changed;
			int count;

			lock (_selectionPadlock)
			{
				var position = FindPosition(_visibleRecords, lineNumber);
				if (position < 0)
				{
					throw new RecordNotFoundException($"Unable to find record. LineNumber={lineNumber}");
				}

				var line = PositionRanges.FromRange(position, position);
				PositionRanges selection = isSelected
					? PositionRanges.Union(_selection, line)
					: PositionRanges.Except(_selection, line);
				changed = System.Math.Abs(selection.Count - _selection.Count);

				_selection = selection;
				_selectionPeriod = CalculateTimePeriod(selection, _visibleRecords);
				count = selection.Count;
			}

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. {(isSelected ? "Added" : "Removed")}={changed}, Current={count}");

			if (isSelected)
			{
				// Always update the active line number to keep navigation in sync, even if the line was already selected.
				_navigationManager.SetActiveLineNumber(lineNumber);
			}

			return this;
		}

		public ISelect SaveSelection(string destinationFilePath, FileFormatType fileFormatType)
		{
			return SaveSelection(destinationFilePath, fileFormatType, System.Threading.CancellationToken.None);
		}

		public ISelect SaveSelection(string destinationFilePath, FileFormatType fileFormatType, System.Threading.CancellationToken cancellationToken)
		{
			// The scope is already in line number order, so it is saved as it is, not sorted into a copy of every record (#1137).
			RecordList sortedRecords = _selection.Count > 1
				? GetSelected()
				: _scope.Records;

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Selected records are being saved to disk...");

			if (sortedRecords != null)
			{
				new DiskWriter(destinationFilePath, _sourceFileEncoding, fileFormatType, _state).Write(sortedRecords, cancellationToken);

				Log.Default.Write(
					LogSeverityType.Trace,
					$"Selected records have been saved to disk. Count={sortedRecords.Count}");
			}

			return this;
		}

		/// <summary>
		/// Returns the selection, the anchor and the active record by line number, so they can outlive this view's records (#1126).
		/// </summary>
		internal Snapshot TakeSnapshot()
		{
			PositionRanges selection;
			RecordList results;
			int anchorLineNumber;

			lock (_selectionPadlock)
			{
				selection = _selection;
				results = _visibleRecords;
				anchorLineNumber = _anchorLineNumber;
			}

			// Wrapped without a copy: nothing else holds the array.
			ImmutableArray<int> lineNumbers = ImmutableCollectionsMarshal.AsImmutableArray(GetLineNumbers(selection, results));

			return new Snapshot(lineNumbers, anchorLineNumber, _navigationManager.ActiveLineNumber);
		}

		/// <summary>
		/// Selects again the records of <paramref name="snapshot"/> that this view shows, and keeps its anchor and active record if shown.
		/// </summary>
		/// <remarks>
		/// Called by Reload before it applies the filter, which then narrows the selection as usual. Records are matched by line number.
		/// </remarks>
		internal void Restore(Snapshot snapshot)
		{
			ImmutableArray<int> lineNumbers = snapshot.SelectedLineNumbers;

			lock (_selectionPadlock)
			{
				// Compared with this engine's records, so records a Clear removed and Reload restored are not selected.
				_selection = lineNumbers.Length == 0
					? new PositionRanges()
					: PositionRanges.Remap(PositionRanges.FromRange(0, lineNumbers.Length - 1), lineNumbers, _visibleRecords, _scope.Records);
				_selectionPeriod = CalculateTimePeriod(_selection, _visibleRecords);
				_anchorLineNumber = FindPosition(_visibleRecords, snapshot.AnchorLineNumber) < 0 ? NoLineNumber : snapshot.AnchorLineNumber;
			}

			SetActiveLineIfShown(snapshot.ActiveLineNumber);
		}

		public ISelect SelectOnly(int anchorLineNumber, int lineNumber)
		{
			return ChangeRange(anchorLineNumber, lineNumber, replacesSelection: true);
		}

		public ISelect SelectRange(int anchorLineNumber, int lineNumber)
		{
			return ChangeRange(anchorLineNumber, lineNumber, replacesSelection: false);
		}

		public ISelect Toggle(int lineNumber)
		{
			int count;

			lock (_selectionPadlock)
			{
				var position = FindPosition(_visibleRecords, lineNumber);
				if (position < 0)
				{
					throw new RecordNotFoundException($"Unable to find record. LineNumber={lineNumber}");
				}

				var record = PositionRanges.FromRange(position, position);
				_selection = _selection.Contains(position)
					? PositionRanges.Except(_selection, record)
					: PositionRanges.Union(_selection, record);
				_selectionPeriod = CalculateTimePeriod(_selection, _visibleRecords);
				_anchorLineNumber = lineNumber;
				count = _selection.Count;
			}

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. Toggled={lineNumber}, Current={count}");

			_navigationManager.SetActiveLineNumber(lineNumber);

			return this;
		}

		public ISelect SelectAll()
		{
			int count;

			lock (_selectionPadlock)
			{
				_selection = PositionRanges.FromRange(0, _visibleRecords.Count - 1);
				_selectionPeriod = CalculateTimePeriod(_selection, _visibleRecords);
				count = _selection.Count;
			}

			Log.Default.Write(
				LogSeverityType.Trace,
				$"Every record the filter shows has been selected. Current={count}");

			return this;
		}

		private ISelect ChangeRange(int anchorLineNumber, int lineNumber, bool replacesSelection)
		{
			int count;

			if (anchorLineNumber == NoLineNumber)
			{
				anchorLineNumber = lineNumber;
			}

			lock (_selectionPadlock)
			{
				var first = PositionRanges.LowerBound(_visibleRecords, System.Math.Min(anchorLineNumber, lineNumber), 0);
				var last = PositionRanges.LowerBound(_visibleRecords, System.Math.Max(anchorLineNumber, lineNumber) + 1, first) - 1;
				var range = PositionRanges.FromRange(first, last);

				_selection = replacesSelection ? range : PositionRanges.Union(_selection, range);
				_selectionPeriod = CalculateTimePeriod(_selection, _visibleRecords);
				_anchorLineNumber = anchorLineNumber;
				count = _selection.Count;
			}

			Log.Default.Write(
				LogSeverityType.Trace,
				$"The number of selected records has changed. AnchorLineNumber={anchorLineNumber}, LineNumber={lineNumber}, Current={count}");

			SetActiveLineIfShown(lineNumber);

			return this;
		}

		private void SetActiveLineIfShown(int lineNumber)
		{
			if (FindPosition(_visibleRecords, lineNumber) >= 0)
			{
				_navigationManager.SetActiveLineNumber(lineNumber);
			}
		}

		/// <summary>
		/// Keeps the selected records that <paramref name="kept"/> holds, at their positions in it, and forgets the anchor,
		/// as a new engine starts (#1018).
		/// </summary>
		/// <remarks>
		/// Clear calls it before its filter shows the kept records, so listeners read a selection that matches those results (#1015).
		/// The positions are found by line number: remapping them would assume the records still include the old results, which Clear removes.
		/// </remarks>
		/// <param name="selected">The records selected before the Clear.</param>
		/// <param name="kept">The records the Clear keeps, which its filter then shows.</param>
		/// <returns>The last selected record kept, or <see cref="Record.Dummy"/> when none is.</returns>
		internal IRecord Narrow(RecordList selected, RecordList kept)
		{
			var lastKept = -1;
			var positions = new List<int>();
			var searchFrom = 0;

			// Found by line number, so only the last selected record kept is created (#1137).
			// Both lists are in line number order, so each search starts after the last record found (#1140).
			for (var index = 0; index < selected.Count; index++)
			{
				var position = PositionRanges.IndexOfLineNumber(kept, selected.GetLineNumber(index), searchFrom);

				if (position >= 0)
				{
					positions.Add(position);
					lastKept = index;
					searchFrom = position + 1;
				}
			}

			lock (_selectionPadlock)
			{
				_visibleRecords = kept;
				_selection = PositionRanges.FromPositions(positions);
				_selectionPeriod = CalculateTimePeriod(_selection, _visibleRecords);
				_anchorLineNumber = NoLineNumber;
			}

			return lastKept >= 0 ? selected[lastKept] : Record.Dummy;
		}

		public RecordList GetSelected()
		{
			RecordList selectedRecords;

			lock (_selectionPadlock)
			{
				selectedRecords = GetRecords(_selection, _visibleRecords);
			}

			Log.Default.Write(
				LogSeverityType.Debug,
				$"The selected records have been requested. Selected={selectedRecords.Count}");

			return selectedRecords;
		}

		public void ToggleIsPinned()
		{
			// Pins are kept by line number, so no record is created (#1137).
			var selectedLineNumbers = GetSelectedLineNumbers();

			if (selectedLineNumbers.Length > 0)
			{
				IAnnotations annotations = _state.Annotations;
				var isPinned = !annotations.IsPinned(selectedLineNumbers[0]);

				// One change to the session, so the grid refreshes once. The lines to change are found in parallel, outside the store's lock.
				var lineNumbers = selectedLineNumbers
					.AsParallel()
					.Where(lineNumber => annotations.IsPinned(lineNumber) != isPinned)
					.ToArray();

				annotations.SetPinned(lineNumbers, isPinned);

				Log.Default.Write(
					LogSeverityType.Information,
					$"Changing record state. IsPinned={isPinned}, RecordsAffected={selectedLineNumbers.Length}");
			}
		}

		public void SetComment(string comment, bool allowOverwrite)
		{
			// White space alone is no comment, so it is not stored: pasted onto every record, it would keep an entry for each line.
			comment = string.IsNullOrWhiteSpace(comment) ? string.Empty : comment;

			// Comments are kept by line number, so no record is created (#1137).
			IAnnotations annotations = _state.Annotations;

			// One change to the session, so the grid refreshes once. The lines to change are found in parallel, outside the store's lock.
			var lineNumbers = GetSelectedLineNumbers()
				.AsParallel()
				.Where(lineNumber => allowOverwrite || !annotations.HasComment(lineNumber))
				.Where(lineNumber => annotations.GetComment(lineNumber) != comment)
				.ToArray();

			annotations.SetComment(lineNumbers, comment);

			Log.Default.Write(
				LogSeverityType.Information,
				$"Commenting the selected records. AllowOverwrite={allowOverwrite}, RecordsAffected={lineNumbers.Length}");
		}

		/// <summary>
		/// A view's selection by line number, which holds none of its records (#1126).
		/// </summary>
		internal sealed record Snapshot(ImmutableArray<int> SelectedLineNumbers, int AnchorLineNumber, int ActiveLineNumber);
	}
}
