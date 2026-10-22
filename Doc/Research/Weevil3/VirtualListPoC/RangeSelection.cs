#nullable enable

namespace Weevil.VirtualListPoC;

/// <summary>
/// An inclusive range of positions in the current filter results.
/// </summary>
internal readonly record struct PositionRange(int First, int Last)
{
	public int Count => this.Last - this.First + 1;
}

/// <summary>
/// The selection, stored as sorted ranges of positions in the current filter results (decision 5 of the #990 plan),
/// so selecting every row costs one range instead of one object per row.
/// </summary>
/// <remarks>
/// Ranges never overlap or touch: adjacent ranges are merged.
/// </remarks>
internal sealed class RangeSelection
{
	private readonly List<PositionRange> _ranges;

	public RangeSelection()
	{
		_ranges = new List<PositionRange>();
	}

	private RangeSelection(List<PositionRange> ranges, long count)
	{
		_ranges = ranges;
		this.Count = count;
	}

	public long Count { get; private set; }

	public int RangeCount => _ranges.Count;

	public IReadOnlyList<PositionRange> Ranges => _ranges;

	public int First => _ranges.Count == 0 ? -1 : _ranges[0].First;

	public int Last => _ranges.Count == 0 ? -1 : _ranges[^1].Last;

	public void Clear()
	{
		_ranges.Clear();
		this.Count = 0;
	}

	public void SelectOnly(int position)
	{
		Clear();
		Add(position, position);
	}

	public void SelectAll(int resultCount)
	{
		Clear();
		if (resultCount > 0)
		{
			Add(0, resultCount - 1);
		}
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

	public void Toggle(int position)
	{
		if (Contains(position))
		{
			Remove(position, position);
		}
		else
		{
			Add(position, position);
		}
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
	/// Keeps the selected records that the new filter still shows, and nothing else.
	/// </summary>
	/// <remarks>
	/// Both result lists are sorted record numbers. For each selected range, two binary searches find the new
	/// results that lie between its first and last record. When the old results hold every record in between,
	/// as they do with no filter, all of those new results were selected: one range, no scan. Otherwise the two
	/// lists are merged, which reads each selected result and each new result in that window once.
	/// </remarks>
	public static RangeSelection Remap(RangeSelection selection, int[] oldResults, int[] newResults, CancellationToken cancellationToken = default)
	{
		var ranges = new List<PositionRange>();
		long count = 0;
		var searchFrom = 0;

		foreach (PositionRange range in selection._ranges)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var firstRecord = oldResults[range.First];
			var lastRecord = oldResults[range.Last];

			var low = LowerBound(newResults, firstRecord, searchFrom);
			var high = LowerBound(newResults, lastRecord + 1, low) - 1;
			if (low > high)
			{
				continue;
			}

			searchFrom = high + 1;

			if (lastRecord - firstRecord == range.Last - range.First)
			{
				Append(ranges, low, high, ref count);
				continue;
			}

			var oldPosition = range.First;
			var newPosition = low;
			while (newPosition <= high && oldPosition <= range.Last)
			{
				var newRecord = newResults[newPosition];
				var oldRecord = oldResults[oldPosition];
				if (newRecord == oldRecord)
				{
					Append(ranges, newPosition, newPosition, ref count);
					newPosition++;
					oldPosition++;
				}
				else if (newRecord < oldRecord)
				{
					newPosition++;
				}
				else
				{
					oldPosition++;
				}

				if ((newPosition & 0xFFFFF) == 0)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}
		}

		return new RangeSelection(ranges, count);
	}

	/// <summary>
	/// Returns the position of the first value that is at least <paramref name="value"/>, or the length if there is none.
	/// </summary>
	internal static int LowerBound(int[] sorted, int value, int from)
	{
		var low = from;
		var high = sorted.Length;
		while (low < high)
		{
			var middle = low + ((high - low) / 2);
			if (sorted[middle] < value)
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

	private static void Append(List<PositionRange> ranges, int first, int last, ref long count)
	{
		if (ranges.Count > 0 && ranges[^1].Last == first - 1)
		{
			ranges[^1] = new PositionRange(ranges[^1].First, last);
		}
		else
		{
			ranges.Add(new PositionRange(first, last));
		}

		count += last - first + 1;
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
