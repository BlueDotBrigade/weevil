namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Collections.Immutable;
	using BlueDotBrigade.Weevil.Navigation;

	public static class RecordExtensions
	{
		/// <summary>
		/// Determines the index value of the <paramref name="record"/> within the provided collection.
		/// </summary>
		/// <param name="record"></param>
		/// <param name="records"></param>
		/// <returns></returns>
		public static int ToIndexUsing(this IRecord record, ImmutableArray<IRecord> records)
		{
			var index = records.IndexOfLineNumber(record.LineNumber);
			return index;
		}

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
		public static int IndexOfLineNumber(ImmutableArray<IRecord> records, int lineNumber, RecordSearchType searchType = RecordSearchType.ExactMatch)
		{
			return RecordSearch.IndexOfLineNumber(records, lineNumber, searchType);
		}

		public static int IndexOfCreatedAt(ImmutableArray<IRecord> records, DateTime createdAt, RecordSearchType searchType = RecordSearchType.ExactMatch)
		{
			return RecordSearch.IndexOfCreatedAt(records, createdAt, searchType);
		}

		/// <summary>
		/// Determines whether the collection has a result with the provided line number.
		/// </summary>
		/// <param name="sourceRecords">A record collection sorted by ascending order.</param>
		/// <param name="lineNumber">The value to search for.</param>
		/// <param name="index">The position of the record in the <paramref name="sourceRecords"/> with the corresponding <paramref name="lineNumber"/>.</param>
		/// <returns>True is returned if the collection has a matching line number.</returns>
		public static bool TryGetIndexOfLineNumber(this ImmutableArray<IRecord> sourceRecords, int lineNumber, out int index)
		{
			return RecordSearch.TryGetIndexOf(sourceRecords, lineNumber, out index);
		}

		/// <summary>
		/// Attempts to find a record that has the same line number as the provided value.
		/// </summary>
		/// <param name="sourceRecords">The list of records to search.</param>
		/// <param name="lineNumber">The line number to search for.</param>
		/// <param name="result">Returns the matching result, or <see cref="Record.Dummy"/></param>
		/// <returns>Returns <see lang="True"/> if a record with a matching line number is found.</returns>
		public static bool TryGetIndexOfLine(this ImmutableArray<IRecord> sourceRecords, int lineNumber, out IRecord result)
		{
			return RecordSearch.TryGetIndexOfLine(sourceRecords, lineNumber, out result);
		}
	}
}
