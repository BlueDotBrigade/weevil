namespace BlueDotBrigade.Weevil
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    internal class RegionManager
    {
        private readonly List<RegionOfInterest> _regions = new List<RegionOfInterest>();
		
        private int? _startIndex = null;

        public ImmutableArray<RegionOfInterest> Regions => _regions.ToImmutableArray();

        public void MarkStart(int recordIndex)
        {
            if (_regions.Any(r => r.Contains(recordIndex)))
            {
                throw new InvalidOperationException($"The record is already contained within an existing region. RecordIndex={recordIndex}");
            }
            _startIndex = recordIndex;
        }

        public void MarkEnd(int recordIndex)
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
                    throw new InvalidOperationException("Unable to create region because this region has already been defined.");
                }

                // Check for overlap with existing regions
                if (_regions.Any(r => r.OverlapsWith(newRegion)))
                {
                    _startIndex = null;
                    throw new InvalidOperationException("Unable to create region because it overlaps with an existing region.");
                }

                _regions.Add(newRegion);
                _startIndex = null;
                return;
            }
            else
            {
                throw new InvalidOperationException("Region start has not been marked.");
            }
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