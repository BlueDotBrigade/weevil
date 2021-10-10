namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data.Comparers;
	using BlueDotBrigade.Weevil.Navigation;

	internal static class RecordSearch
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
		public static int IndexOfLineNumber(ImmutableArray<IRecord> records, int lineNumber, SearchType searchType = SearchType.ExactMatch)
		{
			var desiredRecord = new Record(
				lineNumber,
				Record.CreationTimeUnknown,
				SeverityType.Debug,
				$"This record is used to facilitate binary searching of line number: {lineNumber}");

			return IndexOf(records, desiredRecord, new LineNumberComparer(), searchType);
		}

		public static int IndexOfCreatedAt(ImmutableArray<IRecord> records, DateTime createdAt, SearchType searchType = SearchType.ExactMatch)
		{
			var desiredRecord = new Record(
				0,
				createdAt,
				SeverityType.Debug,
				$"This record is used to facilitate binary searching for a record created at: {createdAt}");

			return IndexOf(records, desiredRecord, new CreatedAtComparer(), searchType);
		}

		private static int IndexOf(ImmutableArray<IRecord> records, IRecord desiredRecord, MagnitudeComparer comparer, SearchType searchType = SearchType.ExactMatch)
		{
			if (records.Length == 0)
			{
				throw new RecordNotFoundException(-1);
			}

			var index = records.BinarySearch(desiredRecord, comparer);

			if (searchType == SearchType.ClosestMatch)
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
						var beforeIndex = Math.Abs(index) - 1;
						var afterIndex = Math.Abs(beforeIndex) - 1;

						var beforeDelta = Math.Abs(comparer.CompareMagnitude(records[beforeIndex], desiredRecord));
						var afterDelta = Math.Abs(comparer.CompareMagnitude(desiredRecord, records[afterIndex]));

						index = afterDelta < beforeDelta ? afterIndex : beforeIndex;
					}
				}
			}

			if (index < 0 || index > records.Length - 1)
			{
				throw new RecordNotFoundException(index);
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
		public static bool TryGetIndexOf(this ImmutableArray<IRecord> sourceRecords, int lineNumber, out int index)
		{
			var desiredRecord = new Record(
				lineNumber,
				Record.CreationTimeUnknown,
				SeverityType.Debug,
				$"This record is used to facilitate binary searching for line number: {lineNumber}");

			index = sourceRecords.BinarySearch(desiredRecord, new LineNumberComparer());
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
		public static bool TryGetLine(this ImmutableArray<IRecord> sourceRecords, int lineNumber, out IRecord result)
		{
			result = Record.Dummy;

			var desiredRecord = new Record(
				lineNumber,
				Record.CreationTimeUnknown,
				SeverityType.Debug,
				$"This record is used to facilitate binary searching for line number: {lineNumber}");

			var index = sourceRecords.BinarySearch(desiredRecord, new LineNumberComparer());
			var wasFound = index >= 0;

			if (wasFound)
			{
				result = sourceRecords[index];
			}

			return wasFound;
		}
	}
}
