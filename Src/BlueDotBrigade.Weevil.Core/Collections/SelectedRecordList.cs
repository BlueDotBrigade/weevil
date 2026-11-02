namespace BlueDotBrigade.Weevil.Collections
{
	using System;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// The selected records, read from the results through the selected ranges, without copying them (#1140).
	/// </summary>
	internal sealed class SelectedRecordList : RecordList
	{
		private readonly RecordList _results;
		private readonly PositionRange[] _ranges;

		// Where each range's first record is in this list.
		private readonly int[] _rangeStarts;

		public SelectedRecordList(PositionRanges selection, RecordList results)
		{
			_results = results ?? throw new ArgumentNullException(nameof(results));
			_ranges = selection.Ranges.ToArray();
			_rangeStarts = new int[_ranges.Length];

			var start = 0;

			for (var index = 0; index < _ranges.Length; index++)
			{
				_rangeStarts[index] = start;
				start += _ranges[index].Count;
			}

			this.Count = start;
		}

		public override int Count { get; }

		public override IRecord this[int position] => _results[ToResultsPosition(position)];

		public override int GetLineNumber(int position) => _results.GetLineNumber(ToResultsPosition(position));

		public override DateTime GetCreatedAt(int position) => _results.GetCreatedAt(ToResultsPosition(position));

		private int ToResultsPosition(int position)
		{
			if ((uint)position >= (uint)this.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(position), position, "The position is expected to be within the selection.");
			}

			var index = Array.BinarySearch(_rangeStarts, position);

			if (index < 0)
			{
				index = ~index - 1;
			}

			return _ranges[index].First + (position - _rangeStarts[index]);
		}
	}
}
