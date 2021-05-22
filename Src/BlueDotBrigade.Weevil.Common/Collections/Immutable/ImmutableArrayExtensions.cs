namespace BlueDotBrigade.Weevil.Collections.Immutable
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Data;

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
		/// Determines whether the collection has a result with the provided line number.
		/// </summary>
		/// <param name="array">The sorted collection (line number ASC) to search.,</param>
		/// <param name="lineNumber">The value to search for.</param>
		/// <returns>True is returned if the collection has a matching line number.</returns>
		public static bool HasLineNumber(this ImmutableArray<IRecord> array, int lineNumber)
		{
			var index = array.BinarySearch(new Record(lineNumber), new RecordLineNumberComparer());
			var wasFound = index >= 0;

			return wasFound;
		}

		/// <summary>
		/// Attempts to find a record that has the same line number as the provided value.
		/// </summary>
		/// <param name="array">The list of records to search.</param>
		/// <param name="lineNumber">The line number to search for.</param>
		/// <param name="result">Returns the matching result, or <see cref="Record.Dummy"/></param>
		/// <returns>Returns <see lang="True"/> if a record with a matching line number is found.</returns>
		public static bool TryGetLine(this ImmutableArray<IRecord> array, int lineNumber, out IRecord result)
		{
			result = Record.Dummy;

			var index = array.BinarySearch(new Record(lineNumber), new RecordLineNumberComparer());
			var wasFound = index >= 0;

			if (wasFound)
			{
				result = array[index];
			}

			return wasFound;
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

		public static (DateTime From, DateTime To) GetRange(this ImmutableArray<IRecord> records)
		{
			var from = Record.CreationTimeUnknown;
			var to = Record.CreationTimeUnknown;

			if (records.Length > 0)
			{
				if (records.Length == 1)
				{
					from = records[0].CreatedAt;
					to = records[0].CreatedAt;
				}
				else
				{
					from = records[0].CreatedAt;
					to = records[records.Length - 1].CreatedAt;
				}
			}

			return (from, to);
		}

		public static (DateTime From, DateTime To) GetEstimatedRange(this ImmutableArray<IRecord> records)
		{
			var from = Record.CreationTimeUnknown;
			var to = Record.CreationTimeUnknown;

			if (records.Length > 0)
			{
				if (records.Length == 1)
				{
					from = EstimateFirstTimestamp(records);
					to = from;
				}
				else
				{
					from = EstimateFirstTimestamp(records);
					to = EstimateLastTimestamp(records);
				}
			}

			return (from, to);
		}
	}
}