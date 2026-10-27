namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Linq;
	using System.Collections.Immutable;
	using System.Diagnostics;

	/// <remarks>
	/// Writes are serialized by the session's lock (#1107). Each write publishes a new array, so reads never wait.
	/// </remarks>
	[DebuggerDisplay("Count={_regions.Length}")]
	internal class RegionManager : IRegionManager
	{
		private readonly object _writeLock;
		private ImmutableArray<Region> _regions = ImmutableArray<Region>.Empty;

		// Read and written while holding the lock.
		private int? _startLineNumber;

		internal RegionManager() : this(new object())
		{
			// nothing to do
		}

		/// <param name="writeLock">The lock that serializes writes, shared by every store of the session.</param>
		internal RegionManager(object writeLock)
		{
			_writeLock = writeLock ?? throw new ArgumentNullException(nameof(writeLock));
		}

		public ImmutableArray<Region> Regions => _regions;

		/// <summary>
		/// Replaces every region, such as with those read from the sidecar.
		/// </summary>
		internal void Load(ImmutableArray<Region> regions)
		{
			lock (_writeLock)
			{
				Publish(regions.IsDefault ? ImmutableArray<Region>.Empty : regions);
			}
		}

		public void CreateFromSelection(string regionName, int[] selectedLineNumbers)
		{
			var sortedLineNumbers = selectedLineNumbers.OrderBy(k => k).ToArray();
			var minLineNumber = sortedLineNumbers.Min();
			var maxLineNumber = sortedLineNumbers.Max();

			lock (_writeLock)
			{
				ImmutableArray<Region> regions = _regions;
				var region = new Region(regionName, minLineNumber, maxLineNumber);

				// Prevent creating the same region twice
				if (regions.Any(r => r.Minimum.LineNumber == region.Minimum.LineNumber && r.Maximum.LineNumber == region.Maximum.LineNumber))
				{
					throw new InvalidOperationException("Unable to create region because this region has already been defined.");
				}

				// Check for overlap with existing regions
				if (regions.Any(r => r.OverlapsWith(region)))
				{
					throw new InvalidOperationException("Unable to create region because it overlaps with an existing region.");
				}

				Publish(regions.Add(region));
			}
		}

		public void MarkStart(int lineNumber)
		{
			lock (_writeLock)
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
			lock (_writeLock)
			{
				if (!_startLineNumber.HasValue)
				{
					throw new InvalidOperationException("Region start has not been marked.");
				}

				ImmutableArray<Region> regions = _regions;
				var start = System.Math.Min(_startLineNumber.Value, lineNumber);
				var end = System.Math.Max(_startLineNumber.Value, lineNumber);

				var newRegion = new Region(start, end);

				// Prevent creating the same region twice
				if (regions.Any(r => r.Minimum == newRegion.Minimum && r.Maximum == newRegion.Maximum))
				{
					_startLineNumber = null;
					throw new InvalidOperationException("Unable to create region because this region has already been defined.");
				}

				// Check for overlap with existing regions
				if (regions.Any(r => r.OverlapsWith(newRegion)))
				{
					_startLineNumber = null;
					throw new InvalidOperationException("Unable to create region because it overlaps with an existing region.");
				}

				Publish(regions.Add(newRegion));
				_startLineNumber = null;
			}
		}

		public bool TryGetRegionName(int lineNumber, out string regionName)
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
			lock (_writeLock)
			{
				Publish(ImmutableArray<Region>.Empty);
			}
		}

		public bool Clear(int lineNumber)
		{
			lock (_writeLock)
			{
				ImmutableArray<Region> regions = _regions;
				Region region = regions.FirstOrDefault(r => r.Contains(lineNumber));

				if (region != null)
				{
					Publish(regions.Remove(region));
					return true;
				}
				return false;
			}
		}

		// Called while holding the lock.
		private void Publish(ImmutableArray<Region> regions)
		{
			ImmutableInterlocked.InterlockedExchange(ref _regions, regions);
		}
	}
}
