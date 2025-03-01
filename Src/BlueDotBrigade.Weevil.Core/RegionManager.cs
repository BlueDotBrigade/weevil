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
		private readonly List<Region> _regions;
		private readonly object _regionsPadlock;

		private int? _startLineNumber;

		internal RegionManager() : this(ImmutableArray<Region>.Empty)
		{
			// nothing to do
		}

		internal RegionManager(ImmutableArray<Region> regions)
		{
			_regions = new List<Region>(regions);
			_regionsPadlock = new object();

			_startLineNumber = null;
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

		public void CreateFromSelection(string regionName, int[] selectedLineNumbers)
		{
			var sortedLineNumbers = selectedLineNumbers.OrderBy(k => k).ToArray();
			var minLineNumber = sortedLineNumbers.Min();
			var maxLineNumber = sortedLineNumbers.Max();

			lock (_regionsPadlock)
			{
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

		public void MarkStart(int lineNumber)
		{
			lock (_regionsPadlock)
			{
				if (_regions.Any(r => r.Contains(lineNumber)))
				{
					throw new InvalidOperationException($"The record is already contained within an existing region. LineNumber={lineNumber}");
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
		
		public bool TryGetRegionName(int lineNumber, out string regionName)
		{			
			lock (_regionsPadlock)
			{
				Region region = _regions.FirstOrDefault(r => r.Contains(lineNumber));

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

		public bool Clear(int lineNumber)
		{
			lock (_regionsPadlock)
			{
				Region region = _regions.FirstOrDefault(r => r.Contains(lineNumber));

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
