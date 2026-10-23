namespace BlueDotBrigade.Weevil.Collections
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// An inclusive range of positions in a view's filter results.
	/// </summary>
	internal readonly record struct PositionRange(int First, int Last)
	{
		public int Count => this.Last - this.First + 1;
	}

	/// <summary>
	/// A set of positions in a view's filter results, stored as sorted ranges (decision 5 of the #990 plan), so that
	/// selecting every record costs one range instead of one entry per record (#1015).
	/// </summary>
	/// <remarks>
	/// Ranges never overlap or touch: adjacent ranges are merged. The proof of concept in
	/// <c>Doc/Research/Weevil3/VirtualListPoC</c> measured this design at 100 million rows (#1014).
	/// A set never changes once it is built. A change builds a new set in one pass, so changing many positions costs
	/// no more than reading both sets.
	/// </remarks>
	internal sealed class PositionRanges
	{
		private readonly List<PositionRange> _ranges;

		public PositionRanges()
		{
			_ranges = new List<PositionRange>();
		}

		public int Count { get; private set; }

		public int RangeCount => _ranges.Count;

		public IReadOnlyList<PositionRange> Ranges => _ranges;

		/// <summary>
		/// The first position, or -1 when the set is empty.
		/// </summary>
		public int First => _ranges.Count == 0 ? -1 : _ranges[0].First;

		/// <summary>
		/// The last position, or -1 when the set is empty.
		/// </summary>
		public int Last => _ranges.Count == 0 ? -1 : _ranges[^1].Last;

		public bool Contains(int position)
		{
			var index = FindRangeAtOrAfter(position);
			return index < _ranges.Count && _ranges[index].First <= position;
		}

		/// <summary>
		/// Returns every position from <paramref name="first"/> to <paramref name="last"/>, or none when <paramref name="last"/> is before <paramref name="first"/>.
		/// </summary>
		public static PositionRanges FromRange(int first, int last)
		{
			var ranges = new PositionRanges();
			if (first <= last)
			{
				ranges.Append(first, last);
			}

			return ranges;
		}

		/// <summary>
		/// Returns the given positions as ranges. They may come in any order, and more than once.
		/// </summary>
		/// <remarks>
		/// Sorts <paramref name="positions"/> in place.
		/// </remarks>
		public static PositionRanges FromPositions(List<int> positions)
		{
			positions.Sort();

			var ranges = new PositionRanges();
			foreach (var position in positions)
			{
				ranges.Append(position, position);
			}

			return ranges;
		}

		/// <summary>
		/// Returns the positions that are in either set, in one pass over both.
		/// </summary>
		public static PositionRanges Union(PositionRanges a, PositionRanges b)
		{
			var union = new PositionRanges();
			var i = 0;
			var j = 0;

			while (i < a._ranges.Count || j < b._ranges.Count)
			{
				PositionRange next = j == b._ranges.Count || (i < a._ranges.Count && a._ranges[i].First <= b._ranges[j].First)
					? a._ranges[i++]
					: b._ranges[j++];

				union.Append(next.First, next.Last);
			}

			return union;
		}

		/// <summary>
		/// Returns the positions of <paramref name="a"/> that are not in <paramref name="b"/>, in one pass over both.
		/// </summary>
		public static PositionRanges Except(PositionRanges a, PositionRanges b)
		{
			var difference = new PositionRanges();
			var j = 0;

			foreach (PositionRange range in a._ranges)
			{
				var first = range.First;

				while (j < b._ranges.Count && b._ranges[j].Last < first)
				{
					j++;
				}

				// A removed range may also cover the next range, so it is only passed once it ends before this one does.
				while (j < b._ranges.Count && b._ranges[j].First <= range.Last)
				{
					PositionRange removed = b._ranges[j];
					if (removed.First > first)
					{
						difference.Append(first, removed.First - 1);
					}

					first = removed.Last + 1;
					if (removed.Last > range.Last)
					{
						break;
					}

					j++;
				}

				if (first <= range.Last)
				{
					difference.Append(first, range.Last);
				}
			}

			return difference;
		}

		public IEnumerable<int> EnumeratePositions()
		{
			foreach (PositionRange range in _ranges)
			{
				for (var position = range.First; position <= range.Last; position++)
				{
					yield return position;
				}
			}
		}

		/// <summary>
		/// Keeps the positions of the records that the new results still show, and nothing else.
		/// </summary>
		/// <param name="selection">Positions in <paramref name="oldResults"/>.</param>
		/// <param name="oldResults">The results the positions refer to, in line number order.</param>
		/// <param name="newResults">The results to map the positions to, in line number order.</param>
		/// <param name="allRecords">Every record the view holds, in line number order; both results are subsets of it.</param>
		/// <remarks>
		/// For each range, two binary searches find the new results between its first and last record. When the old
		/// results held every record in between, as they do with no filter, all of those new results were selected: one
		/// range, no scan. Otherwise the two lists are merged, which reads each selected record and each new result in
		/// that stretch once.
		/// </remarks>
		public static PositionRanges Remap(
			PositionRanges selection,
			ImmutableArray<IRecord> oldResults,
			ImmutableArray<IRecord> newResults,
			ImmutableArray<IRecord> allRecords)
		{
			var remapped = new PositionRanges();
			var searchFrom = 0;

			foreach (PositionRange range in selection._ranges)
			{
				var firstLine = oldResults[range.First].LineNumber;
				var lastLine = oldResults[range.Last].LineNumber;

				var low = LowerBound(newResults, firstLine, searchFrom);
				var high = LowerBound(newResults, lastLine + 1, low) - 1;
				if (low > high)
				{
					continue;
				}

				searchFrom = high + 1;

				// The log must still hold both ends: after Reload, a log that shrank no longer holds every old result (#1062).
				var firstInLog = LowerBound(allRecords, firstLine, 0);
				var lastInLog = LowerBound(allRecords, lastLine, firstInLog);
				var spanInLog = lastInLog - firstInLog + 1;
				if (spanInLog == range.Count && lastInLog < allRecords.Length && allRecords[lastInLog].LineNumber == lastLine)
				{
					remapped.Append(low, high);
					continue;
				}

				var oldPosition = range.First;
				var newPosition = low;
				while (newPosition <= high && oldPosition <= range.Last)
				{
					var newLine = newResults[newPosition].LineNumber;
					var oldLine = oldResults[oldPosition].LineNumber;
					if (newLine == oldLine)
					{
						remapped.Append(newPosition, newPosition);
						newPosition++;
						oldPosition++;
					}
					else if (newLine < oldLine)
					{
						newPosition++;
					}
					else
					{
						oldPosition++;
					}
				}
			}

			return remapped;
		}

		/// <summary>
		/// Returns the position of the first record whose line number is at least <paramref name="lineNumber"/>,
		/// or the length if there is none.
		/// </summary>
		internal static int LowerBound(ImmutableArray<IRecord> records, int lineNumber, int from)
		{
			var low = from;
			var high = records.Length;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (records[middle].LineNumber < lineNumber)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			return low;
		}

		/// <summary>
		/// Adds positions that start at or after the first position of the last range, merging with it when they overlap or touch.
		/// </summary>
		private void Append(int first, int last)
		{
			if (_ranges.Count > 0 && _ranges[^1].Last >= first - 1)
			{
				PositionRange previous = _ranges[^1];
				if (last > previous.Last)
				{
					_ranges[^1] = new PositionRange(previous.First, last);
					this.Count += last - previous.Last;
				}
			}
			else
			{
				_ranges.Add(new PositionRange(first, last));
				this.Count += last - first + 1;
			}
		}

		/// <summary>
		/// Returns the index of the first range whose last position is at least <paramref name="position"/>.
		/// </summary>
		private int FindRangeAtOrAfter(int position)
		{
			var low = 0;
			var high = _ranges.Count;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (_ranges[middle].Last < position)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			return low;
		}
	}
}
