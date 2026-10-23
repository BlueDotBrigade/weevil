namespace BlueDotBrigade.Weevil.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// The selected records by line number, read from a selection stored as ranges (#1015), so callers of
	/// <see cref="ISelect.Selected"/> keep working without a copy of every selected record.
	/// </summary>
	/// <remarks>
	/// It reads one moment of the selection: later changes create a new <see cref="PositionRanges"/>, never change this one.
	/// <see cref="Count"/> is immediate, a lookup by line number is a binary search, and enumerating creates nothing per record.
	/// </remarks>
	internal sealed class SelectedRecords : IDictionary<int, IRecord>
	{
		private readonly PositionRanges _selection;
		private readonly ImmutableArray<IRecord> _results;

		public SelectedRecords(PositionRanges selection, ImmutableArray<IRecord> results)
		{
			_selection = selection;
			_results = results;
		}

		public int Count => _selection.Count;

		public bool IsReadOnly => true;

		public ICollection<int> Keys => new ReadOnlyView<int>(this.Select(pair => pair.Key), this.Count, lineNumber => ContainsKey(lineNumber));

		public ICollection<IRecord> Values => new ReadOnlyView<IRecord>(this.Select(pair => pair.Value), this.Count, record => record != null && TryGetValue(record.LineNumber, out IRecord found) && ReferenceEquals(found, record));

		public IRecord this[int lineNumber]
		{
			get => TryGetValue(lineNumber, out IRecord record) ? record : throw new KeyNotFoundException($"Line {lineNumber} is not selected.");
			set => throw new NotSupportedException("The selection is read-only here; use ISelect to change it.");
		}

		public bool ContainsKey(int lineNumber)
		{
			return TryGetValue(lineNumber, out _);
		}

		public bool TryGetValue(int lineNumber, out IRecord record)
		{
			var position = PositionRanges.LowerBound(_results, lineNumber, 0);
			if (position < _results.Length && _results[position].LineNumber == lineNumber && _selection.Contains(position))
			{
				record = _results[position];
				return true;
			}

			record = null;
			return false;
		}

		public bool Contains(KeyValuePair<int, IRecord> item)
		{
			return TryGetValue(item.Key, out IRecord record) && ReferenceEquals(record, item.Value);
		}

		public IEnumerator<KeyValuePair<int, IRecord>> GetEnumerator()
		{
			foreach (var position in _selection.EnumeratePositions())
			{
				IRecord record = _results[position];
				yield return new KeyValuePair<int, IRecord>(record.LineNumber, record);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void CopyTo(KeyValuePair<int, IRecord>[] array, int arrayIndex)
		{
			foreach (KeyValuePair<int, IRecord> pair in this)
			{
				array[arrayIndex++] = pair;
			}
		}

		public void Add(int key, IRecord value) => throw new NotSupportedException();

		public void Add(KeyValuePair<int, IRecord> item) => throw new NotSupportedException();

		public bool Remove(int key) => throw new NotSupportedException();

		public bool Remove(KeyValuePair<int, IRecord> item) => throw new NotSupportedException();

		public void Clear() => throw new NotSupportedException();

		/// <summary>
		/// The keys or values, read as they are enumerated.
		/// </summary>
		private sealed class ReadOnlyView<T> : ICollection<T>
		{
			private readonly IEnumerable<T> _items;
			private readonly Func<T, bool> _contains;

			public ReadOnlyView(IEnumerable<T> items, int count, Func<T, bool> contains)
			{
				_items = items;
				_contains = contains;
				this.Count = count;
			}

			public int Count { get; }

			public bool IsReadOnly => true;

			public bool Contains(T item) => _contains(item);

			public void CopyTo(T[] array, int arrayIndex)
			{
				foreach (T item in _items)
				{
					array[arrayIndex++] = item;
				}
			}

			public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			public void Add(T item) => throw new NotSupportedException();

			public bool Remove(T item) => throw new NotSupportedException();

			public void Clear() => throw new NotSupportedException();
		}
	}
}
