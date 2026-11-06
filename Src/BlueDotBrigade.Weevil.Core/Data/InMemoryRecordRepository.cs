namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using Diagnostics;

	internal class InMemoryRecordRepository : IRecordRepository
	{
		private const int OperationInactive = 0;
		private const int OperationRunning = 1;

		#region Fields
		private readonly RecordList _allRecords;
		private readonly RecordList _visibleRecords;
		private readonly RecordList _selectedRecords;

		private readonly ImmutableArray<Region> _regions;

		private readonly ClearOperation _clearOperation;
		private int _operationState;
		#endregion

		#region Object Lifetime
		/// <summary>
		/// Repository is used to clear unwanted records from memory.
		/// </summary>
		/// <param name="allRecords">A collection of records sorted in ascending order by <see cref="LineNumber"/>.</param>
		/// <param name="selectedRecords">A collection of user selected records, sorted in ascending order by <see cref="LineNumber"/></param>
		/// <param name="clearOperation">Determines which records will be cleared from memory.</param>
		/// <remarks>
		/// This operation will not impact records that have already been persisted to disk.
		/// </remarks>
		public InMemoryRecordRepository(
			RecordList allRecords,
			RecordList visibleRecords,
			RecordList selectedRecords,
			ClearOperation clearOperation,
			ImmutableArray<Region> regions)
		{
			_allRecords = allRecords;
			_visibleRecords = visibleRecords;
			_selectedRecords = selectedRecords;

			_regions = regions;

			_clearOperation = clearOperation;
			_operationState = OperationInactive;
		}
		#endregion

		#region Properties
		#endregion

		#region Private Methods
		/// <summary>
		/// Returns the lowest and highest line numbers of <paramref name="records"/>, read without creating records.
		/// </summary>
		private static (int Lowest, int Highest) GetLineNumberRange(RecordList records)
		{
			var lowest = int.MaxValue;
			var highest = int.MinValue;

			for (var position = 0; position < records.Count; position++)
			{
				var lineNumber = records.GetLineNumber(position);
				lowest = System.Math.Min(lowest, lineNumber);
				highest = System.Math.Max(highest, lineNumber);
			}

			return (lowest, highest);
		}

		/// <summary>
		/// Clears all records from memory leading up to, but not including, the first selected item.
		/// </summary>
		/// <remarks>
		/// The first record is based on the associated line number, not the order in which the records were selected.
		///
		/// The active filters have no impact on this operation.
		///
		/// When complete, the original log file will remain unchanged.
		/// </remarks>
		private static List<int> ClearBeforeSelected(RecordList records, RecordList selectedRecords)
		{
			var results = new List<int>();

			if (records.Length > 0 && selectedRecords.Length > 0)
			{
				var lowestSelectedLine = GetLineNumberRange(selectedRecords).Lowest;

				for (var position = 0; position < records.Count; position++)
				{
					if (records.GetLineNumber(position) >= lowestSelectedLine)
					{
						results.Add(position);
					}
				}
			}

			return results;
		}

		/// <summary>
		/// Clears all records from memory that follow the last selected item.
		/// </summary>
		/// <remarks>
		/// The last record is based on the associated line number, not the order in which the records were selected.
		///
		/// The active filters have no impact on this operation.
		///
		/// When complete, the original log file will remain unchanged.
		/// </remarks>
		private static List<int> ClearAfterSelected(RecordList records, RecordList selectedRecords)
		{
			var results = new List<int>();

			if (records.Length > 0 && selectedRecords.Length > 0)
			{
				var highestSelectedLine = GetLineNumberRange(selectedRecords).Highest;

				for (var position = 0; position < records.Count; position++)
				{
					if (records.GetLineNumber(position) <= highestSelectedLine)
					{
						results.Add(position);
					}
					else
					{
						break;
					}
				}
			}

			return results;
		}

		private static List<int> ClearBeforeAndAfterSelected(RecordList records, RecordList selectedRecords)
		{
			var results = new List<int>();

			if (records.Length > 0 && selectedRecords.Length > 0)
			{
				(var lowestSelectedLine, var highestSelectedLine) = GetLineNumberRange(selectedRecords);

				for (var position = 0; position < records.Count; position++)
				{
					var lineNumber = records.GetLineNumber(position);

					if (lineNumber < lowestSelectedLine)
					{
						continue;
					}

					if (lineNumber > highestSelectedLine)
					{
						break;
					}

					results.Add(position);
				}
			}

			return results;
		}

		private static List<int> ClearBetweenSelected(RecordList records, RecordList selectedRecords)
		{
			var results = new List<int>();

			if (records.Length >= 2 && selectedRecords.Length >= 2)
			{
				(var lowestSelectedLine, var highestSelectedLine) = GetLineNumberRange(selectedRecords);

				for (var position = 0; position < records.Count; position++)
				{
					var lineNumber = records.GetLineNumber(position);

					if (lineNumber <= lowestSelectedLine || lineNumber >= highestSelectedLine)
					{
						results.Add(position);
					}
				}
			}

			return results;
		}

		/// <summary>
		/// Selected records are cleared from memory.
		/// </summary>
		/// <remarks>
		/// When complete, the original log file will remain unchanged.
		/// </remarks>
		private static List<int> ClearSelected(RecordList records, RecordList selectedRecords)
		{
			var results = new List<int>(records.Length - selectedRecords.Length);

			var blacklist = Enumerable.Range(0, selectedRecords.Count).Select(selectedRecords.GetLineNumber).ToImmutableHashSet();

			for (var position = 0; position < records.Count; position++)
			{
				if (!blacklist.Contains(records.GetLineNumber(position)))
				{
					results.Add(position);
				}
			}

			return results;
		}

		/// <summary>
		/// Selected records will be kept, while all others will be cleared from memory.
		/// </summary>
		/// <remarks>
		/// Records that have been hidden by the active filters will not be impacted by this operation.
		/// 
		/// When complete, the original log file will remain unchanged.
		/// </remarks>
		private static List<int> ClearUnselected(RecordList records, RecordList selectedRecords)
		{
			var results = new List<int>(selectedRecords.Count);
			var searchFrom = 0;

			// The selected records are in line number order, as the records are, so each search starts after the last record found.
			// Selected records are often next to each other, as after Select All, so the next record is tried before a search.
			for (var index = 0; index < selectedRecords.Count; index++)
			{
				var lineNumber = selectedRecords.GetLineNumber(index);
				var position = searchFrom < records.Count && records.GetLineNumber(searchFrom) == lineNumber
					? searchFrom
					: Collections.PositionRanges.LowerBound(records, lineNumber, searchFrom);

				if (position < records.Count && records.GetLineNumber(position) == lineNumber)
				{
					results.Add(position);
					searchFrom = position + 1;
				}
			}

			return results;
		}

		/// <summary>
		/// Returns <see langword="true"/> when <paramref name="positions"/> are 0, 1, 2 and so on, up to <paramref name="count"/>: every record, in order.
		/// </summary>
		private static bool IsEveryPosition(List<int> positions, int count)
		{
			if (positions.Count != count)
			{
				return false;
			}

			for (var index = 0; index < count; index++)
			{
				if (positions[index] != index)
				{
					return false;
				}
			}

			return true;
		}

		/// <returns>The positions of the records kept, or <see langword="null"/> to keep every record.</returns>
		private static List<int> ClearBeyondRegions(RecordList allRecords, ImmutableArray<Region> regions)
		{
			// Have any regions of interest been defined?
			// ... If not, then return everything.
			if (regions.Length == 0)
			{
				return null;
			}
			else
			{
				var results = new List<int>();

				// Sort regions by line number to maintain sequential order (Bug #647)
				foreach (Region region in regions.OrderBy(r => r.Minimum.LineNumber))
				{
					// Add all records that fall within the current region of interest
					var position = Collections.PositionRanges.LowerBound(allRecords, region.Minimum.LineNumber, 0);

					for (; position < allRecords.Count && allRecords.GetLineNumber(position) <= region.Maximum.LineNumber; position++)
					{
						results.Add(position);
					}
				}

				return results;
			}
		}
		#endregion

		#region Event Handlers
		#endregion

		public IRecord GetNext()
		{
			throw new NotImplementedException();
		}

		/// <remarks>
		/// The records kept are found by line number, and kept as positions in the records, so no record is created (#1140).
		/// </remarks>
		public RecordList Get(int maxRecords)
		{
			if (System.Threading.Interlocked.Exchange(ref _operationState, OperationRunning) == OperationRunning)
			{
				throw new InvalidOperationException("An operation is already in progress.");
			}

			// Null keeps every record.
			List<int> keptPositions = null;

			try
			{
				// Early exit?
				if (_selectedRecords.Length == 0 && _clearOperation != ClearOperation.BeyondRegions)
				{
					keptPositions = null;
				}
				else
				{
					switch (_clearOperation)
					{
						case ClearOperation.BeforeSelected:
							keptPositions = ClearBeforeSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.BeforeAndAfterSelected:
							keptPositions = ClearBeforeAndAfterSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.BetweenSelected:
							keptPositions = ClearBetweenSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.AfterSelected:
							keptPositions = ClearAfterSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.Selected:
							keptPositions = ClearSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.Unselected:
							keptPositions = ClearUnselected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.BeyondRegions:
							keptPositions = ClearBeyondRegions(_allRecords, _regions);
							break;

						default:
							throw new ArgumentOutOfRangeException(nameof(_clearOperation), _clearOperation, "Clear operation is not supported.");
					}

					// A Clear that keeps every record, as after Select All, keeps the list rather than copying its table.
					if (keptPositions != null && IsEveryPosition(keptPositions, _allRecords.Length))
					{
						keptPositions = null;
					}

					var recordsCleared = _allRecords.Length - (keptPositions?.Count ?? _allRecords.Length);

					if (recordsCleared > 0)
					{
						Log.Default.Write(LogSeverityType.Information,
							"Records have been cleared.",
							new Dictionary<string, object>
							{
								{"Operation", _clearOperation.ToString()},
								{"Cleared", recordsCleared.ToString()},
							});
					}
				}

				if (maxRecords != int.MinValue &&
					maxRecords != int.MaxValue)
				{
					var keptCount = keptPositions?.Count ?? _allRecords.Length;

					if (keptCount >= maxRecords)
					{
						keptPositions = keptPositions?.Take(maxRecords).ToList() ?? Enumerable.Range(0, maxRecords).ToList();
					}

					Log.Default.Write(LogSeverityType.Information,
						$"Record results have been limited to... Maximum={maxRecords}");
				}
			}
			finally
			{
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
				_operationState = OperationInactive;
			}

			// A list kept whole is returned as it is, not copied. Otherwise the records kept get a table of their own,
			// so the records cleared, and the chunks only they were read from, are freed (#1140).
			return keptPositions == null
				? _allRecords
				: TableRecordList.Keep(_allRecords, keptPositions.ToArray());
		}

		public RecordList Get(Range range, int maximumCount)
		{
			throw new NotImplementedException();
		}

		public RecordList GetAll()
		{
			return Get(_allRecords.Length);
		}
	}
}