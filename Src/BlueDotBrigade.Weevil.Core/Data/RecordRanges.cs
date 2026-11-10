namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Splits a list's positions into ranges for parallel readers, such as a filter's workers (#1027).
	/// </summary>
	internal static class RecordRanges
	{
		/// <summary>
		/// Returns ranges that cover every position of <paramref name="records"/> once, in order. Each range starts at <c>Start</c> and ends before <c>End</c>.
		/// </summary>
		/// <param name="alignToChunks">
		/// The records' content is read from the log, so a range starts at each content chunk's first record, and a worker reads each chunk once.
		/// Only over a whole record table whose records read chunks.
		/// </param>
		/// <param name="workerCount">How many workers read the ranges at once.</param>
		/// <remarks>
		/// - Along chunks, a log with fewer chunks than workers has each chunk's range split, so every worker has one.
		///   Each worker then reads the chunk once, so a log smaller than <paramref name="workerCount"/> chunks is read up to twice that many chunks' worth.
		/// - Otherwise, as in RAM mode, where reading a chunk costs nothing, the list is split into about 4 ranges per worker, of about the same size.
		/// </remarks>
		public static (int Start, int End)[] Split(RecordList records, bool alignToChunks, int workerCount)
		{
			ArgumentNullException.ThrowIfNull(records);
			ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);

			var ranges = new List<(int Start, int End)>();

			if (alignToChunks && records is TableRecordList { ListsWholeTable: true } list && list.Table.ChunkStarts.Count > 0)
			{
				var start = 0;

				foreach (var chunkStart in list.Table.ChunkStarts)
				{
					if (chunkStart > start)
					{
						ranges.Add((start, chunkStart));
						start = chunkStart;
					}
				}

				if (start < records.Count)
				{
					ranges.Add((start, records.Count));
				}

				if (ranges.Count < workerCount)
				{
					var partsPerChunk = (workerCount + ranges.Count - 1) / ranges.Count;
					var chunkRanges = ranges.ToArray();
					ranges.Clear();

					foreach ((int Start, int End) chunkRange in chunkRanges)
					{
						AddEqualRanges(ranges, chunkRange.Start, chunkRange.End, partsPerChunk);
					}
				}
			}
			else
			{
				AddEqualRanges(ranges, 0, records.Count, 4 * workerCount);
			}

			return ranges.ToArray();
		}

		/// <summary>
		/// Adds up to <paramref name="count"/> ranges of about the same size, from <paramref name="start"/> to <paramref name="end"/>.
		/// </summary>
		private static void AddEqualRanges(List<(int Start, int End)> ranges, int start, int end, int count)
		{
			var rangeSize = System.Math.Max(1, (end - start + count - 1) / count);

			for (var rangeStart = start; rangeStart < end; rangeStart += rangeSize)
			{
				ranges.Add((rangeStart, System.Math.Min(rangeStart + rangeSize, end)));
			}
		}
	}
}
