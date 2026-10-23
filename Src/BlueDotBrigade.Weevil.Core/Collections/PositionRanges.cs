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
	/// </remarks>
	internal sealed class PositionRanges
	{
		private readonly List<PositionRange> _ranges;

		public PositionRanges()
		{
			_ranges = new List<PositionRange>();
		}

		private PositionRanges(List<PositionRange> ranges, int count)
		{
			_ranges = ranges;
			this.Count = count;
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

		public PositionRanges Clone()
		{
			return new PositionRanges(new List<PositionRange>(_ranges), this.Count);
		}

		public bool Contains(int position)
		{
			var index = FindRangeAtOrAfter(position);
			return index < _ranges.Count && _ranges[index].First <= position;
		}

		/// <summary>
		/// Adds every position from <paramref name="first"/> to <paramref name="last"/>, in either order.
		/// </summary>
		public void Add(int first, int last)
		{
			if (first > last)
			{
				(first, last) = (last, first);
			}

			// Every range that overlaps or touches the new one is merged into it.
			var start = FindRangeAtOrAfter(first - 1);
			var end = start;
			while (end < _ranges.Count && _ranges[end].First <= last + 1)
			{
				first = System.Math.Min(first, _ranges[end].First);
				last = System.Math.Max(last, _ranges[end].Last);
				this.Count -= _ranges[end].Count;
				end++;
			}

			_ranges.RemoveRange(start, end - start);
			_ranges.Insert(start, new PositionRange(first, last));
			this.Count += last - first + 1;
		}

		/// <summary>
		/// Removes every position from <paramref name="first"/> to <paramref name="last"/>, in either order.
		/// </summary>
		public void Remove(int first, int last)
		{
			if (first > last)
			{
				(first, last) = (last, first);
			}

			var start = FindRangeAtOrAfter(first);
			var end = start;
			var kept = new List<PositionRange>(2);
			while (end < _ranges.Count && _ranges[end].First <= last)
			{
				PositionRange range = _ranges[end];
				this.Count -= range.Count;
				if (range.First < first)
				{
					kept.Add(new PositionRange(range.First, first - 1));
				}

				if (range.Last > last)
				{
					kept.Add(new PositionRange(last + 1, range.Last));
				}

				end++;
			}

			_ranges.RemoveRange(start, end - start);
			_ranges.InsertRange(start, kept);
			foreach (PositionRange range in kept)
			{
				this.Count += range.Count;
			}
		}

		public void Clear()
		{
			_ranges.Clear();
			this.Count = 0;
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

				var spanInLog = LowerBound(allRecords, lastLine, 0) - LowerBound(allRecords, firstLine, 0) + 1;
				if (spanInLog == range.Count)
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
		/// Adds positions after every existing range, merging with the last range when they touch.
		/// </summary>
		private void Append(int first, int last)
		{
			if (_ranges.Count > 0 && _ranges[^1].Last == first - 1)
			{
				_ranges[^1] = new PositionRange(_ranges[^1].First, last);
			}
			else
			{
				_ranges.Add(new PositionRange(first, last));
			}

			this.Count += last - first + 1;
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
