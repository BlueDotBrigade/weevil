namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Navigation;

	public static class RecordCollectionExtensions
	{
		private const int AttemptCount = 16;

		private static DateTime EstimateFirstTimestamp(ImmutableArray<IRecord> sourceRecords)
		{
			var result = Record.CreationTimeUnknown;

			var maxIndex = sourceRecords.Length < AttemptCount
				? sourceRecords.Length
				: AttemptCount;

			for (var i = 0; i < maxIndex; i++)
			{
				if (sourceRecords[i].HasCreationTime)
				{
					result = sourceRecords[i].CreatedAt;
					break;
				}
			}

			return result;
		}

		private static DateTime EstimateLastTimestamp(ImmutableArray<IRecord> sourceRecords)
		{
			var result = Record.CreationTimeUnknown;

			var minIndex = sourceRecords.Length > AttemptCount
				? sourceRecords.Length - AttemptCount - 1
				: 0;

			for (var i = sourceRecords.Length-1; minIndex <= i; i--)
			{
				if (sourceRecords[i].HasCreationTime)
				{
					result = sourceRecords[i].CreatedAt;
					break;
				}
			}

			return result;
		}

		/// <summary>
		/// Compresses the size of the array by removing default values (e.g. null).
		/// </summary>
		/// <typeparam name="T">The array type.</typeparam>
		/// <param name="sourceRecords">The array to be compressed.</param>
		/// <param name="count">The number of non-default items in the array.</param>
		/// <returns>A new array that does not include default (e.g. `null`) values.</returns>
		public static ImmutableArray<T> Compact<T>(this ImmutableArray<T> sourceRecords, int count)
		{
			var compressedResults = new T[count];

			var insertAt = 0;

			for (var i = 0; i < sourceRecords.Length; i++)
			{
				if (EqualityComparer<T>.Default.Equals(sourceRecords[i], default(T)))
				{
					// points to null
				}
				else
				{
					compressedResults[insertAt] = sourceRecords[i];
					insertAt++;
				}
			}

			return ImmutableArray.Create(compressedResults);
		}

		/// <summary>
		/// Retrieves the index of the record with a matching line number.
		/// </summary>
		/// <param name="sourceRecords">The list of records to search.</param>
		/// <param name="lineNumber">The line number to search for.</param>
		/// <param name="searchType">Indicates what is considered an acceptable result.</param>
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
		/// <remarks>
		/// This method is faster than a sequential search.
		/// </remarks>
		public static int IndexOfLineNumber(this ImmutableArray<IRecord> sourceRecords, int lineNumber, RecordSearchType searchType = RecordSearchType.ExactMatch)
		{
			return RecordSearch.IndexOfLineNumber(sourceRecords, lineNumber, searchType);
		}

		/// <summary>
		/// Retrieves the index of the record with a matching <paramref name="lineNumber"/>.
		/// </summary>
		/// <param name="sourceRecords">A record collection sorted by ascending order.</param>
		/// <param name="lineNumber">The value to search for.</param>
		/// <param name="index">The position of the record in the <paramref name="sourceRecords"/> with the corresponding <paramref name="lineNumber"/>.</param>
		/// <returns>True is returned if the collection has a matching line number.</returns>
		/// <remarks>
		/// This method is faster than a sequential search.
		/// </remarks>
		public static bool TryIndexOfLineNumber(this ImmutableArray<IRecord> sourceRecords, int lineNumber, out int index)
		{
			return RecordSearch.TryIndexOfLineNumber(sourceRecords, lineNumber, out index);
		}

		/// <summary>
		/// Retrieves a record that has a matching <paramref name="lineNumber"/>.
		/// </summary>
		/// <param name="sourceRecords">The list of records to search.</param>
		/// <param name="lineNumber">The line number to search for.</param>
		/// <param name="result">Returns the matching result, or <see cref="Record.Dummy"/></param>
		/// <returns>Returns <see lang="True"/> if a record with a matching line number is found.</returns>
		/// <remarks>
		/// This method is faster than a sequential search.
		/// </remarks>
		/// <exception cref="RecordNotFoundException"/>
		public static IRecord RecordAtLineNumber(this ImmutableArray<IRecord> sourceRecords, int lineNumber)
		{
			IRecord result = Record.Dummy;

			if (RecordSearch.TryRecordOfLineNumber(sourceRecords, lineNumber, out var record))
			{
				result = record;
			}
			else
			{
				throw new RecordNotFoundException($"Unable to find record. LineNumber={lineNumber}");
			}

			return result;
		}

		/// <summary>
		/// Retrieves a record that has a matching <paramref name="lineNumber"/>.
		/// </summary>
		/// <param name="sourceRecords">The list of records to search.</param>
		/// <param name="lineNumber">The line number to search for.</param>
		/// <param name="result">Returns the matching result, or <see cref="Record.Dummy"/></param>
		/// <returns>Returns <see lang="True"/> if a record with a matching line number is found.</returns>
		/// <remarks>
		/// This method is faster than a sequential search.
		/// </remarks>
		public static bool TryRecordOfLineNumber(this ImmutableArray<IRecord> sourceRecords, int lineNumber, out IRecord result)
		{
			return RecordSearch.TryRecordOfLineNumber(sourceRecords, lineNumber, out result);
		}

		/// <summary>
		/// Attempts to find a record that has the same line number as the provided value.
		/// </summary>
		/// <param name="sourceRecords">The list of records to search.</param>
		/// <param name="createdAt">The timestamp to search for.</param>
		/// <param name="searchType">Indicates what is considered an acceptable result.</param>
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
		/// <remarks>
		/// This method is faster than a sequential search.
		/// </remarks>
		public static int IndexOfCreatedAt(this ImmutableArray<IRecord> sourceRecords, DateTime createdAt, RecordSearchType searchType = RecordSearchType.ExactMatch)
		{
			return RecordSearch.IndexOfCreatedAt(sourceRecords, createdAt, searchType);
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

		/// <summary>
		/// Returns a reference to the first result.
		/// </summary>
		public static IRecord First(this ImmutableArray<IRecord> sourceRecords)
		{
			return sourceRecords[0];
		}

		/// <summary>
		/// Returns a reference to the last result.
		/// </summary>
		public static IRecord Last(this ImmutableArray<IRecord> sourceRecords)
		{
			return sourceRecords[sourceRecords.Length - 1];
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

		/// <summary>
		/// Navigates through records in descending order (e.g. lines: 8, 5, 3, 2).
		/// </summary>
		/// <param name="sourceRecords">The record collection to search.</param>
		/// <param name="startAt">The index to start searching from. Defaults to zero (0) when <paramref name="startAt"/> is -1.</param>
		/// <param name="checkIfMatches"></param>
		/// <returns>
		/// Returns the <see cref="Data.Record"/> that matches the search criteria.
		/// </returns>
		/// <exception cref="RecordNotFoundException"/>
		public static int GoToPrevious(this ImmutableArray<IRecord> sourceRecords, int startAt, Func<IRecord, bool> checkIfMatches)
		{
			const int UnknownActiveRecord = -1;

			if (sourceRecords.Length == 0)
			{
				return UnknownActiveRecord;
			}
			else
			{
				var wasFound = false;
				var index = startAt >= 0 ? startAt : UnknownActiveRecord;

				var indexOfResult = UnknownActiveRecord;

				for (var i = 0; i < sourceRecords.Length; i++)
				{
					index = index - 1 < 0 ? sourceRecords.Length - 1 : index - 1;

					if (checkIfMatches(sourceRecords[index]))
					{
						indexOfResult = index;
						wasFound = true;
						break;
					}
				}

				return wasFound
					? indexOfResult
					: throw new RecordNotFoundException();
			}
		}

		/// <summary>
		/// Navigates through records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <param name="sourceRecords">Collection to search.</param>
		/// <param name="startAt">The index to start searching from. Defaults to zero (0) when <paramref name="startAt"/> is -1.</param>
		/// <param name="checkIfMatches">The condition used for comparison.</param>
		/// <returns>
		/// Returns the <see cref="Data.Record"/> that matches the search criteria.
		/// </returns>
		/// <exception cref="RecordNotFoundException"/>
		public static int GoToNext(this ImmutableArray<IRecord> sourceRecords, int startAt, Func<IRecord, bool> checkIfMatches)
		{
			const int UnknownActiveRecord = -1;

			if (sourceRecords.Length == 0)
			{
				return UnknownActiveRecord;
			}
			else
			{
				var index = startAt >= 0 ? startAt : UnknownActiveRecord;
				var wasFound = false;

				var indexOfResult = UnknownActiveRecord;

				for (var i = 0; i < sourceRecords.Length; i++)
				{
					index = (index + 1) % sourceRecords.Length;

					if (checkIfMatches(sourceRecords[index]))
					{
						indexOfResult = index;
						wasFound = true;
						break;
					}
				}

				return wasFound
					? indexOfResult
					: throw new RecordNotFoundException();
			}
		}

		public static IEnumerable<IRecord> GetSectionRecords(this ImmutableArray<IRecord> records, ISection section, ITableOfContents tableOfContent)
		{
			var firstLineNumber = section.LineNumber;

			var firstIndex = records.IndexOfLineNumber(
				firstLineNumber, 
				RecordSearchType.ExactOrNext);

			ISection nextSection = tableOfContent
				.Sections
				.Where(s => s.LineNumber > section.LineNumber)
				.OrderBy(s => s.LineNumber)
				.FirstOrDefault();

			var lastLineNumber = (nextSection != null)
				? nextSection.LineNumber
				: records[records.Length - 1].LineNumber;

			var lastIndex = records.IndexOfLineNumber(
				lastLineNumber, 
				RecordSearchType.ExactOrPrevious);

			for (var i = firstIndex; i <= lastIndex; i++)
			{
				if (records[i].LineNumber >= section.LineNumber && 
					records[i].LineNumber < nextSection.LineNumber)
				{
					yield return records[i];
				}
				else
				{
					break;
				}
			}
		}
	}
}