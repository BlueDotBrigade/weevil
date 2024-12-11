namespace BlueDotBrigade.Weevil
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Threading;

	[DebuggerDisplay("Count={_bookends.Count}")]
	internal class BookendManager : IBookendManager
	{
		private readonly ISelect _selectionManager;

		private readonly List<Bookend> _bookends;

		private int? _startLineNumber = null;

		private int _highestName;

		internal BookendManager(ISelect selectionManager) : this (selectionManager, ImmutableArray<Bookend>.Empty)
		{
			// nothing to do
		}

		internal BookendManager(ISelect selectionManager, ImmutableArray<Bookend> bookends)
		{
			_selectionManager = selectionManager ?? throw new ArgumentNullException(nameof(selectionManager));

			_bookends = new List<Bookend>(bookends);

			_highestName = _bookends.Count == 0
				? 0
				: bookends.Select(bookend => int.Parse(bookend.Name)).Max();
		}

		public ImmutableArray<Bookend> Bookends => _bookends.ToImmutableArray();

		public void CreateFromSelection()
		{
			if (_selectionManager.HasSelectionPeriod)
			{
				var sortedLineNumbers = _selectionManager.Selected.Keys.OrderBy(k => k).ToArray();

				_highestName = Interlocked.Increment(ref _highestName);
				var minLineNumber = sortedLineNumbers.Min();
				var maxLineNumber = sortedLineNumbers.Max();
				var bookend = new Bookend(_highestName.ToString(), minLineNumber, maxLineNumber);

				// Prevent creating the same region twice
				if (_bookends.Any(r => r.Minimum.LineNumber == bookend.Minimum.LineNumber && r.Maximum.LineNumber == bookend.Maximum.LineNumber))
				{
					throw new InvalidOperationException("Unable to create region because this region has already been defined.");
				}
				// Check for overlap with existing regions
				if (_bookends.Any(r => r.OverlapsWith(bookend)))
				{
					throw new InvalidOperationException("Unable to create region because it overlaps with an existing region.");
				}
				_bookends.Add(bookend);
			}
		}
		
		public void MarkStart(int lineNumber)
		{
			if (_bookends.Any(r => r.Contains(lineNumber)))
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
				if (_bookends.Any(r => r.Minimum == newRegion.Minimum && r.Maximum == newRegion.Maximum))
				{
					_startLineNumber = null;
					throw new InvalidOperationException("Unable to create region because this region has already been defined.");
				}

				// Check for overlap with existing regions
				if (_bookends.Any(r => r.OverlapsWith(newRegion)))
				{
					_startLineNumber = null;
					throw new InvalidOperationException("Unable to create region because it overlaps with an existing region.");
				}

				_bookends.Add(newRegion);
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
			_bookends.Clear();
		}

		public bool Clear(int recordIndex)
		{
			var region = _bookends.FirstOrDefault(r => r.Contains(recordIndex));

			if (region != null)
			{
				_bookends.Remove(region);
				return true;
			}
			return false;
		}
	}
}
