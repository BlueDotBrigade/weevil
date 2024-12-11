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
		private readonly ImmutableArray<IRecord> _allRecords;
		private readonly ImmutableArray<IRecord> _visibleRecords;
		private readonly ImmutableArray<IRecord> _selectedRecords;

		private readonly ImmutableArray<Bookend> _bookends;

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
			ImmutableArray<IRecord> allRecords,
			ImmutableArray<IRecord> visibleRecords,
			ImmutableArray<IRecord> selectedRecords,
			ClearOperation clearOperation,
			ImmutableArray<Bookend> bookends)
		{
			_allRecords = allRecords;
			_visibleRecords = visibleRecords;
			_selectedRecords = selectedRecords;

			_bookends = bookends;

			_clearOperation = clearOperation;
			_operationState = OperationInactive;
		}
		#endregion

		#region Properties
		#endregion

		#region Private Methods
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
		private static IList<IRecord> ClearBeforeSelected(ImmutableArray<IRecord> records, ImmutableArray<IRecord> selectedRecords)
		{
			var results = new List<IRecord>();

			if (records.Length > 0 && selectedRecords.Length > 0)
			{
				var lowestSelectedLine = selectedRecords.OrderBy(x => x.LineNumber).First().LineNumber;

				foreach (IRecord record in records)
				{
					if (record.LineNumber < lowestSelectedLine)
					{
						// record will no longer be kept in memory
					}
					else
					{
						results.Add(record);
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
		private static IList<IRecord> ClearAfterSelected(ImmutableArray<IRecord> records, ImmutableArray<IRecord> selectedRecords)
		{
			var results = new List<IRecord>();

			if (records.Length > 0 && selectedRecords.Length > 0)
			{
				var highestSelectedLine = selectedRecords.OrderBy(x => x.LineNumber).Last().LineNumber;

				foreach (IRecord record in records)
				{
					if (record.LineNumber <= highestSelectedLine)
					{
						results.Add(record);
					}
					else
					{
						break;
					}
				}
			}

			return results;
		}

		private static IList<IRecord> ClearBeforeAndAfterSelected(ImmutableArray<IRecord> records, ImmutableArray<IRecord> selectedRecords)
		{
			IList<IRecord> headlessResults = ClearBeforeSelected(records, selectedRecords);

			var immutableHeadlessResults = ImmutableArray.Create(headlessResults.ToArray());

			return ClearAfterSelected(immutableHeadlessResults, selectedRecords);
		}

		private static IList<IRecord> ClearBetweenSelected(ImmutableArray<IRecord> records, ImmutableArray<IRecord> selectedRecords)
		{
			var results = new List<IRecord>();

			if (records.Length >= 2 && selectedRecords.Length >= 2)
			{
				var sortedRecords = selectedRecords.OrderBy(x => x.LineNumber);

				var lowestSelectedLine = sortedRecords.First().LineNumber;
				var highestSelectedLine = sortedRecords.Last().LineNumber;

				foreach (IRecord record in records)
				{
					if (record.LineNumber <= lowestSelectedLine || record.LineNumber >= highestSelectedLine)
					{
						results.Add(record);
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
		private static IList<IRecord> ClearSelected(ImmutableArray<IRecord> records, ImmutableArray<IRecord> selectedRecords)
		{
			var results = new IRecord[records.Length - selectedRecords.Length];

			var blacklist = selectedRecords.ToImmutableHashSet();

			var insertAt = 0;

			for (var i = 0; i <= results.Length; i++)
			{
				if (blacklist.Contains(records[i]))
				{
					// consider the record cleared
				}
				else
				{
					results[insertAt] = records[i];
					insertAt++;
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
		private static IList<IRecord> ClearUnselected(ImmutableArray<IRecord> records, ImmutableArray<IRecord> selectedRecords)
		{
			return selectedRecords;
		}

		private static IList<IRecord> ClearBeyondBookends(ImmutableArray<IRecord> allRecords, ImmutableArray<Bookend> bookends)
		{
			// Have any regions of interest been defined?
			// ... If not, then return everything.
			if (bookends.Length == 0)
			{
				return allRecords;
			}
			else
			{
				var filteredRecords = new List<IRecord>();

				foreach (Bookend bookend in bookends)
				{
					// Add all records that fall within the current region of interest
					filteredRecords.AddRange(allRecords.Where(record =>
						record.LineNumber >= bookend.Minimum.LineNumber &&
						record.LineNumber <= bookend.Maximum.LineNumber));
				}

				return filteredRecords;
			}
		}
		#endregion

		#region Event Handlers
		#endregion

		public IRecord GetNext()
		{
			throw new NotImplementedException();
		}

		public ImmutableArray<IRecord> Get(int maxRecords)
		{
			if (System.Threading.Interlocked.Exchange(ref _operationState, OperationRunning) == OperationRunning)
			{
				throw new InvalidOperationException("An operation is already in progress.");
			}

			IList<IRecord> visibleRecords = ImmutableArray<IRecord>.Empty;

			try
			{
				// Early exit?
				if (_selectedRecords.Length == 0 && _clearOperation != ClearOperation.BeyondBookends)
				{
					visibleRecords = _allRecords;
				}
				else
				{
					switch (_clearOperation)
					{
						case ClearOperation.BeforeSelected:
							visibleRecords = ClearBeforeSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.BeforeAndAfterSelected:
							visibleRecords = ClearBeforeAndAfterSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.BetweenSelected:
							visibleRecords = ClearBetweenSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.AfterSelected:
							visibleRecords = ClearAfterSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.Selected:
							visibleRecords = ClearSelected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.Unselected:
							visibleRecords = ClearUnselected(_allRecords, _selectedRecords);
							break;

						case ClearOperation.BeyondBookends:
							visibleRecords = ClearBeyondBookends(_allRecords, _bookends);
							break;

						default:
							throw new ArgumentOutOfRangeException(nameof(_clearOperation), _clearOperation, "Clear operation is not supported.");
					}

					var recordsCleared = _allRecords.Length - visibleRecords.Count;

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
					visibleRecords = visibleRecords.Count < maxRecords
						? visibleRecords
						: visibleRecords.Take(maxRecords).ToList();

					Log.Default.Write(LogSeverityType.Information,
						$"Record results have been limited to... Maximum={maxRecords}");
				}
			}
			finally
			{
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
				_operationState = OperationInactive;
			}

			return ImmutableArray.Create(visibleRecords.ToArray());
		}

		public ImmutableArray<IRecord> Get(Range range, int maximumCount)
		{
			throw new NotImplementedException();
		}

		public ImmutableArray<IRecord> GetAll()
		{
			return Get(_allRecords.Length);
		}
	}
}