namespace BlueDotBrigade.Weevil.Collections.Immutable
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Navigation;

	public static class ImmutableArrayExtensions
	{
		private const int AttemptCount = 16;

		private static DateTime EstimateFirstTimestamp(ImmutableArray<IRecord> records)
		{
			var result = Record.CreationTimeUnknown;

			var maxIndex = records.Length < AttemptCount
				? records.Length
				: AttemptCount;

			for (var i = 0; i < maxIndex; i++)
			{
				if (records[i].HasCreationTime)
				{
					result = records[i].CreatedAt;
					break;
				}
			}

			return result;
		}

		private static DateTime EstimateLastTimestamp(ImmutableArray<IRecord> records)
		{
			var result = Record.CreationTimeUnknown;

			var minIndex = records.Length > AttemptCount
				? records.Length - AttemptCount - 1
				: 0;

			for (var i = records.Length-1; minIndex <= i; i--)
			{
				if (records[i].HasCreationTime)
				{
					result = records[i].CreatedAt;
					break;
				}
			}

			return result;
		}

		/// <summary>
		/// Compresses the size of the array by removing default values (e.g. null).
		/// </summary>
		/// <typeparam name="T">The array type.</typeparam>
		/// <param name="source">The array to be compressed.</param>
		/// <param name="count">The number of non-default items in the array.</param>
		/// <returns>A new array that does not include default (e.g. `null`) values.</returns>
		public static ImmutableArray<T> Compact<T>(this ImmutableArray<T> source, int count)
		{
			var compressedResults = new T[count];

			var insertAt = 0;

			for (var i = 0; i < source.Length; i++)
			{
				if (EqualityComparer<T>.Default.Equals(source[i], default(T)))
				{
					// points to null
				}
				else
				{
					compressedResults[insertAt] = source[i];
					insertAt++;
				}
			}

			return ImmutableArray.Create(compressedResults);
		}

		/// <summary>
		/// Attempts to find a record that has the same line number as the provided value.
		/// </summary>
		/// <param name="sourceRecords">The list of records to search.</param>
		/// <param name="lineNumber">The line number to search for.</param>
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
		public static int IndexOfLineNumber(this ImmutableArray<IRecord> sourceRecords, int lineNumber, RecordSearchType searchType = RecordSearchType.ExactMatch)
		{
			return RecordSearch.IndexOfLineNumber(sourceRecords, lineNumber, searchType);
		}

		/// <summary>
		/// Attempts to find a record that has the same line number as the provided value.
		/// </summary>
		/// <param name="sourceRecords">The list of records to search.</param>
		/// <param name="createdAt">The timestamp to search for.</param>
		/// <returns>
		/// <para>
		/// The index of the specified <paramref name="createdAt"/> in the array, if <paramref name="createdAt"/> can be found.
		/// </para>
		/// <para>
		/// If <paramref name="createdAt"/> is not found and <paramref name="createdAt"/> is less than one or more elements in array,
		/// a negative number which is the bitwise complement of the index of the first
		/// element that is larger than <paramref name="createdAt"/>.
		/// </para>
		/// <para>
		/// If <paramref name="createdAt"/> is not found and <paramref name="createdAt"/> is greater
		/// than any of the elements in array, a negative number which is the bitwise
		/// complement of (the index of the last element plus 1).</para>
		/// </returns>
		public static int IndexOfCreatedAt(this ImmutableArray<IRecord> sourceRecords, DateTime createdAt, RecordSearchType searchType = RecordSearchType.ExactMatch)
		{
			return RecordSearch.IndexOfCreatedAt(sourceRecords, createdAt, searchType);
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
			return RecordSearch.TryGetIndexOf(sourceRecords, lineNumber, out index);
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
			return RecordSearch.TryGetIndexOfLine(sourceRecords, lineNumber, out result);
		}

		/// <summary>
		/// Returns a reference to the first result.
		/// </summary>
		public static IRecord First(this ImmutableArray<IRecord> array)
		{
			return array[0];
		}

		/// <summary>
		/// Returns a reference to the last result.
		/// </summary>
		public static IRecord Last(this ImmutableArray<IRecord> array)
		{
			return array[array.Length - 1];
		}

		public static (DateTime From, DateTime To) GetRange(this ImmutableArray<IRecord> sourceRecords)
		{
			var from = Record.CreationTimeUnknown;
			var to = Record.CreationTimeUnknown;

			if (sourceRecords.Length > 0)
			{
				if (sourceRecords.Length == 1)
				{
					from = sourceRecords[0].CreatedAt;
					to = sourceRecords[0].CreatedAt;
				}
				else
				{
					from = sourceRecords[0].CreatedAt;
					to = sourceRecords[sourceRecords.Length - 1].CreatedAt;
				}
			}

			return (from, to);
		}

		public static (DateTime From, DateTime To) GetEstimatedRange(this ImmutableArray<IRecord> sourceRecords)
		{
			var from = Record.CreationTimeUnknown;
			var to = Record.CreationTimeUnknown;

			if (sourceRecords.Length > 0)
			{
				if (sourceRecords.Length == 1)
				{
					from = EstimateFirstTimestamp(sourceRecords);
					to = from;
				}
				else
				{
					from = EstimateFirstTimestamp(sourceRecords);
					to = EstimateLastTimestamp(sourceRecords);
				}
			}

			return (from, to);
		}

		public static IRecord GetFirstCreatedAt(this ImmutableArray<IRecord> sourceRecords)
		{
			IRecord result = Record.Dummy;

			for (int i = 0; i < sourceRecords.Length; i++)
			{
				if (sourceRecords[i].HasCreationTime)
				{
					result = sourceRecords[i];
					break;
				}
			}

			return result;
		}
	}
}