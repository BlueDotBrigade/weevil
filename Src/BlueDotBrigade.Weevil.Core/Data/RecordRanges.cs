namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Splits a list's positions into ranges whose records read the same content chunk, so parallel readers, such as a filter's workers,
	/// read each chunk of a log on disk once (#1027).
	/// </summary>
	internal static class RecordRanges
	{
		/// <summary>
		/// The size of the ranges of a list that does not hold every record of a record table, such as a list built by a test.
		/// </summary>
		public const int DefaultRangeSize = 65_536;

		/// <summary>
		/// Returns ranges that cover every position of <paramref name="records"/> once, in order. Each range starts at <c>Start</c> and ends before <c>End</c>.
		/// </summary>
		/// <remarks>
		/// Over a whole record table, a range starts at each chunk's first record. A table whose records read no chunk, and any other list, is split every
		/// <see cref="DefaultRangeSize"/> positions.
		/// </remarks>
		public static (int Start, int End)[] Split(RecordList records)
		{
			ArgumentNullException.ThrowIfNull(records);

			var ranges = new List<(int Start, int End)>();

			if (records is TableRecordList { ListsWholeTable: true } list && list.Table.ChunkStarts.Count > 0)
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
			}
			else
			{
				for (var start = 0; start < records.Count; start += DefaultRangeSize)
				{
					ranges.Add((start, System.Math.Min(start + DefaultRangeSize, records.Count)));
				}
			}

			return ranges.ToArray();
		}
	}
}
