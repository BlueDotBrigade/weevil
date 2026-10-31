namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Navigation.Comparers;

	internal sealed class RecordSearch
	{
		/// <summary>
		/// Attempts to find a record that has the same line number as the provided value.
		/// </summary>
		/// <param name="records">The collection of records to search.</param>
		/// <param name="lineNumber">The line number to search for.</param>
		/// <param name="searchType">Influences the accuracy of the result.</param>
		/// <returns>
		/// <para>
		/// The index of the specified <paramref name="lineNumber"/> in the array, if <paramref name="lineNumber"/> is found.
		/// </para>
		/// <para>
		/// If <paramref name="lineNumber"/> is not found and <paramref name="lineNumber"/> is less than one or more elements in array,
		/// a negative number which is the bitwise complement of the index of the first
		/// element that is larger than <paramref name="lineNumber"/>.
		/// </para>
		/// <para>
		/// If <paramref name="lineNumber"/> is not found and <paramref name="lineNumber"/> is greater
		/// than any of the elements in array, a negative number which is the bitwise
		/// complement of (the index of the last element plus 1).</para>
		/// </returns>
		public static int IndexOfLineNumber(RecordList records, int lineNumber, RecordSearchType searchType = RecordSearchType.ExactMatch)
		{
			return IndexOf(records, lineNumber, new LineNumberComparer(), searchType);
		}

		public static int IndexOfCreatedAt(RecordList records, DateTime createdAt, RecordSearchType searchType = RecordSearchType.ExactMatch)
		{
			return IndexOf(records, createdAt, new CreatedAtComparer(), searchType);
		}

		/// <summary>
		/// Returns a zero-based index value of the first <see cref="Record"/> that matches the <paramref name="comparer"/>.
		/// </summary>
		/// <param name="records">The sorted <see cref="Record"/> collection to search.</param>
		/// <param name="desiredValue">The line number or timestamp to search for.</param>
		/// <param name="comparer">A comparer that returns the difference between the <paramref name="desiredValue"/>, and the value it reads from any given position of <paramref name="records"/>.</param>
		/// <param name="searchType">Indicates what is considered an acceptable result.</param>
		/// <remarks>
		/// The comparer reads values by position, so the search creates no record (#1137).
		/// </remarks>
		/// <exception cref="RecordNotFoundException"/>
		private static int IndexOf<T>(RecordList records, T desiredValue, MagnitudeComparer<T> comparer, RecordSearchType searchType = RecordSearchType.ExactMatch)
		{
			if (records.Length == 0)
			{
				throw new RecordNotFoundException($"Unable to find record. Value={desiredValue}");
			}

			var index = BinarySearch(records, desiredValue, comparer);

			if (searchType == RecordSearchType.ExactOrPrevious)
			{
				// Unable to find exact match?
				// ... then return next closest value
				if (index < 0)
				{
					index = (index * -1) - 2;
				}
			} 
			else if (searchType == RecordSearchType.ExactOrNext)
			{
				// Unable to find exact match?
				// ... then return next closest value
				if (index < 0)
				{
					index = (index * -1) - 1;
				}
			}
			else if (searchType == RecordSearchType.NearestNeighbor)
			{
				// Unable to find exact match?
				if (index < 0)
				{
					if (records.Length == 1)
					{
						index = 0; // return the first & only record
					}
					// desired value less than first value in array?
					else if (index == -1)
					{
						index = 0;
					}
					// Is desired value greater than last value in array?
					else if (Math.Abs(index) - 1 == records.Length)
					{
						index = records.Length - 1;
					}
					else
					{
						// The index needs to take into consideration that this is a zero based array.
						const int ZeroOffsetAdjustment = -1;

						// Consider the following: if the user searched for 10:31 then the closest match would be Record#8.
						// [Record 8] created at 10:30
						// [Record 9] created at 11:00
						var precedingIndex = Math.Abs(index) - 1 + ZeroOffsetAdjustment;
						var currentIndex = Math.Abs(index) - 0 + ZeroOffsetAdjustment;

						var precedingDelta = Math.Abs(comparer.CompareMagnitude(desiredValue, comparer.Read(records, precedingIndex)));
						var currentDelta = Math.Abs(comparer.CompareMagnitude(comparer.Read(records, currentIndex), desiredValue));

						// Is the desired value (e.g. 10:31) closer to the current record,
						// ... or the one that preceded it (e.g. Record#8)?
						index = precedingDelta < currentDelta ? precedingIndex : currentIndex;
					}
				}
			}

			if (index < 0 || index > records.Length - 1)
			{
				throw new RecordNotFoundException($"Unable to find record. Value={desiredValue}");
			}

			return index;
		}

		/// <summary>
		/// Determines whether the collection has a result with the provided line number.
		/// </summary>
		/// <param name="sourceRecords">A record collection sorted by ascending order.</param>
		/// <param name="lineNumber">The value to search for.</param>
		/// <param name="index">The position of the record in the <paramref name="sourceRecords"/> with the corresponding <paramref name="lineNumber"/>.</param>
		/// <returns>True is returned if the collection has a matching line number.</returns>
		public static bool TryIndexOfLineNumber(RecordList sourceRecords, int lineNumber, out int index)
		{
			index = BinarySearch(sourceRecords, lineNumber, new LineNumberComparer());
			var wasFound = index >= 0;

			return wasFound;
		}

		/// <summary>
		/// Attempts to find a record that has the same line number as the provided value.
		/// </summary>
		/// <param name="sourceRecords">The list of records to search.</param>
		/// <param name="lineNumber">The line number to search for.</param>
		/// <param name="result">Returns the matching result, or <see cref="Record.Dummy"/></param>
		/// <returns>Returns <see lang="True"/> if a record with a matching line number is found.</returns>
		public static bool TryRecordOfLineNumber(RecordList sourceRecords, int lineNumber, out IRecord result)
		{
			result = Record.Dummy;

			var index = BinarySearch(sourceRecords, lineNumber, new LineNumberComparer());
			var wasFound = index >= 0;

			if (wasFound)
			{
				result = sourceRecords[index];
			}

			return wasFound;
		}

		/// <summary>
		/// Searches records ordered by <paramref name="comparer"/>, as <see cref="ImmutableArray{T}.BinarySearch(T, IComparer{T})"/> does:
		/// returns the position of a match, or the bitwise complement of where <paramref name="value"/> would go (#1136).
		/// It reads only the compared values, so it creates no record (#1137).
		/// </summary>
		private static int BinarySearch<T>(RecordList records, T value, MagnitudeComparer<T> comparer)
		{
			var low = 0;
			var high = records.Count - 1;

			while (low <= high)
			{
				var middle = low + ((high - low) >> 1);
				var comparison = comparer.Compare(comparer.Read(records, middle), value);

				if (comparison == 0)
				{
					return middle;
				}

				if (comparison < 0)
				{
					low = middle + 1;
				}
				else
				{
					high = middle - 1;
				}
			}

			return ~low;
		}
	}
}
