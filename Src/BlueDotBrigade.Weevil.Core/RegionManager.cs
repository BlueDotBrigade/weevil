namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;

	[DebuggerDisplay("Count={_regions.Count}")]
	internal class RegionManager : IRegionManager
	{
		private readonly ISelect _selectionManager;

		private readonly List<Region> _regions;
		private readonly object _regionsPadlock;

		private int? _startLineNumber;

		internal RegionManager(ISelect selectionManager) : this(selectionManager, ImmutableArray<Region>.Empty)
		{
			// nothing to do
		}

		internal RegionManager(ISelect selectionManager, ImmutableArray<Region> regions)
		{
			_selectionManager = selectionManager ?? throw new ArgumentNullException(nameof(selectionManager));

			_regions = new List<Region>(regions);
			_regionsPadlock = new object();

			_startLineNumber = null;
		}

		private static string ConvertNumberToLetter(int value)
		{
			if (value < 1 || value > 26)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Input must be between 1 and 26.");
			}

			return ((char)(value + 64)).ToString(); // ASCII 'A' = 65
		}

		public ImmutableArray<Region> Regions
		{
			get
			{
				lock (_regionsPadlock)
				{
					return _regions.ToImmutableArray();
				}
			}
		}

		public void CreateFromSelection()
		{
			if (_selectionManager.HasSelectionPeriod)
			{
				var sortedLineNumbers = _selectionManager.Selected.Keys.OrderBy(k => k).ToArray();
				var minLineNumber = sortedLineNumbers.Min();
				var maxLineNumber = sortedLineNumbers.Max();

				lock (_regionsPadlock)
				{
					var regionName = ConvertNumberToLetter(_regions.Count + 1);
					var region = new Region(regionName, minLineNumber, maxLineNumber);

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
		}

		public void MarkStart(int lineNumber)
		{
			lock (_regionsPadlock)
			{
				if (_regions.Any(r => r.Contains(lineNumber)))
				{
					throw new InvalidOperationException($"The record is already contained within an existing region. RecordIndex={lineNumber}");
				}
				_startLineNumber = lineNumber;
			}
		}

		public void MarkEnd(int lineNumber)
		{
			if (_startLineNumber.HasValue)
			{
				lock (_regionsPadlock)
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
				}
			}
			else
			{
				throw new InvalidOperationException("Region start has not been marked.");
			}
		}

		public bool TryStartsWith(int lineNumber, out string regionName)
		{
			lock (_regionsPadlock)
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
		}

		public bool TryEndsWith(int lineNumber, out string regionName)
		{
			lock (_regionsPadlock)
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
		}

		public bool Contains(int lineNumber)
		{
			lock (_regionsPadlock)
			{
				return _regions.Any(r => r.Contains(lineNumber));
			}
		}

		public void Clear()
		{
			lock (_regionsPadlock)
			{
				_regions.Clear();
			}
		}

		public bool Clear(int recordIndex)
		{
			lock (_regionsPadlock)
			{
				Region region = _regions.FirstOrDefault(r => r.Contains(recordIndex));

				if (region != null)
				{
					_regions.Remove(region);
					return true;
				}
				return false;
			}
		}
	}
}
