namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	internal class RegionManager
	{
		private readonly List<RegionOfInterest> _regions = new List<RegionOfInterest>();
		private int? _startIndex = null;

		public void MarkStart(int recordIndex)
		{
			_startIndex = recordIndex;
		}

		public bool MarkEnd(int recordIndex)
		{
			if (_startIndex.HasValue)
			{
				var start = Math.Min(_startIndex.Value, recordIndex);
				var end = Math.Max(_startIndex.Value, recordIndex);

				var newRegion = new RegionOfInterest(start, end);

				// Prevent creating the same region twice
				if (_regions.Any(r => r.StartIndex == newRegion.StartIndex && r.EndIndex == newRegion.EndIndex))
				{
					_startIndex = null;
					return false;
				}

				// Check for overlap with existing regions
				if (_regions.Any(r => r.OverlapsWith(newRegion)))
				{
					_startIndex = null;
					return false;
				}

				_regions.Add(newRegion);
				_startIndex = null;
				return true;
			}
			else
			{
				throw new InvalidOperationException("Region start has not been marked.");
			}
		}

		public void DeleteRegions()
		{
			_regions.Clear();
		}

		public bool Delete(int recordIndex)
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