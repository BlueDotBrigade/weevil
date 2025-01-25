namespace BlueDotBrigade.Weevil
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Threading;

	[DebuggerDisplay("Count={_regions.Count}")]
	internal class RegionManager : IRegionManager
	{
		private readonly ISelect _selectionManager;

		private readonly List<Region> _regions;

		private int? _startLineNumber = null;

		private int _highestName;

		internal RegionManager(ISelect selectionManager) : this (selectionManager, ImmutableArray<Region>.Empty)
		{
			// nothing to do
		}

		internal RegionManager(ISelect selectionManager, ImmutableArray<Region> regions)
		{
			_selectionManager = selectionManager ?? throw new ArgumentNullException(nameof(selectionManager));

			_regions = new List<Region>(regions);

			_highestName = _regions.Count == 0
				? 0
				: regions.Select(region => int.Parse(region.Name)).Max();
		}

		public ImmutableArray<Region> Regions => _regions.ToImmutableArray();

		public void CreateFromSelection()
		{
			if (_selectionManager.HasSelectionPeriod)
			{
				var sortedLineNumbers = _selectionManager.Selected.Keys.OrderBy(k => k).ToArray();

				_highestName = Interlocked.Increment(ref _highestName);
				var minLineNumber = sortedLineNumbers.Min();
				var maxLineNumber = sortedLineNumbers.Max();
				var region = new Region(_highestName.ToString(), minLineNumber, maxLineNumber);

				// Prevent creating the same region twice
				if (_regions.Any(r => r.Minimum.LineNumber == region.Minimum.LineNumber && r.Maximum.LineNumber == region.Maximum.LineNumber))
				{
					throw new InvalidOperationException("Unable to create region because this region has already been defined.");
				}
				// Check for overlap with existing regions
				if (_regions.Any(r => r.OverlapsWith(region)))
				{
					throw new InvalidOperationException("Unable to create region because it overlaps with an existing region.");
				}
				_regions.Add(region);
			}
		}
		
		public void MarkStart(int lineNumber)
		{
			if (_regions.Any(r => r.Contains(lineNumber)))
			{
				throw new InvalidOperationException($"The record is already contained within an existing region. RecordIndex={lineNumber}");
			}
			_startLineNumber = lineNumber;
		}

		public void MarkEnd(int lineNumber)
		{
			if (_startLineNumber.HasValue)
			{
				var start = Math.Min(_startLineNumber.Value, lineNumber);
				var end = Math.Max(_startLineNumber.Value, lineNumber);

				var newRegion = new Region(start, end);

				// Prevent creating the same region twice
				if (_regions.Any(r => r.Minimum == newRegion.Minimum && r.Maximum == newRegion.Maximum))
				{
					_startLineNumber = null;
					throw new InvalidOperationException("Unable to create region because this region has already been defined.");
				}

				// Check for overlap with existing regions
				if (_regions.Any(r => r.OverlapsWith(newRegion)))
				{
					_startLineNumber = null;
					throw new InvalidOperationException("Unable to create region because it overlaps with an existing region.");
				}

				_regions.Add(newRegion);
				_startLineNumber = null;
				return;
			}
			else
			{
				throw new InvalidOperationException("Region start has not been marked.");
			}
		}

        public bool TryStartsWith(int lineNumber, out string regionName)
        {
			Region region = _regions.FirstOrDefault(r => r.Minimum.LineNumber == lineNumber);

            if (region == null)
            {
				regionName = string.Empty;
				return false;
			}
			else
			{
				regionName = region.Name;
				return true;
			}
        }

		public bool TryEndsWith(int lineNumber, out string regionName)
		{
			Region region = _regions.FirstOrDefault(r => r.Maximum.LineNumber == lineNumber);

			if (region == null)
			{
				regionName = string.Empty;
				return false;
			}
			else
			{
				regionName = region.Name;
				return true;
			}
		}

		public bool Contains(int lineNumber)
		{
			return _regions.Any(r => r.Contains(lineNumber));
		}

		public void Clear()
		{
			_regions.Clear();
		}

		public bool Clear(int recordIndex)
		{
			var region = _regions.FirstOrDefault(r => r.Contains(recordIndex));

			if (region != null)
			{
				_regions.Remove(region);
				return true;
			}
			return false;
		}
	}
}
