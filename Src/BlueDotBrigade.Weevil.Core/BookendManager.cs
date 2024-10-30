namespace BlueDotBrigade.Weevil
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    internal class BookendManager
    {
        private readonly List<Bookend> _regions = new List<Bookend>();
		
        private int? _startLineNumber = null;

        public ImmutableArray<Bookend> Bookends => _regions.ToImmutableArray();

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

                var newRegion = new Bookend(start, end);

                // Prevent creating the same region twice
                if (_regions.Any(r => r.StartLineNumber == newRegion.StartLineNumber && r.EndLineNumber == newRegion.EndLineNumber))
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