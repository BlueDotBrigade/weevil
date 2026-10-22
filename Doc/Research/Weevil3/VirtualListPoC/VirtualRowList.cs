#nullable enable

namespace Weevil.VirtualListPoC;

using System.Collections;
using System.Collections.Specialized;

/// <summary>
/// How often WPF asked the list for something. A count that grows with the number of rows, rather than
/// with the rows on screen, means WPF is walking the whole list.
/// </summary>
internal sealed class LookupCounters
{
	public long Indexer;
	public long IndexOf;
	public long Contains;
	public long Count;
	public long Enumerations;
	public long CopyTo;
	public long RowsCreated;

	public LookupCounters Snapshot()
	{
		return (LookupCounters)MemberwiseClone();
	}

	public LookupCounters Subtract(LookupCounters earlier)
	{
		return new LookupCounters
		{
			Indexer = this.Indexer - earlier.Indexer,
			IndexOf = this.IndexOf - earlier.IndexOf,
			Contains = this.Contains - earlier.Contains,
			Count = this.Count - earlier.Count,
			Enumerations = this.Enumerations - earlier.Enumerations,
			CopyTo = this.CopyTo - earlier.CopyTo,
			RowsCreated = this.RowsCreated - earlier.RowsCreated,
		};
	}
}

/// <summary>
/// The grid's items: the current filter results, as a read-only list that creates a row only when WPF asks for it.
/// </summary>
/// <remarks>
/// <para>
/// It implements the non-generic <see cref="IList"/>: WPF wraps an <see cref="IList"/> in a <c>ListCollectionView</c>
/// that reads through it, while any other <see cref="IEnumerable"/> is copied item by item.
/// </para>
/// <para>
/// <see cref="IndexOf"/> is a binary search by record number, which orders rows as line numbers do.
/// </para>
/// <para>
/// Rows are cached by position through weak references, so while WPF holds a row it gets the same object back,
/// and a change to a line can reach the row on screen. The cache never keeps a row alive.
/// </para>
/// </remarks>
internal sealed class VirtualRowList : IList, INotifyCollectionChanged
{
	private const int PurgeThreshold = 4_096;

	private readonly AnnotationStore _annotations;
	private readonly Func<int, bool> _isSelected;
	private readonly Dictionary<int, WeakReference<RowViewModel>> _rows = new Dictionary<int, WeakReference<RowViewModel>>();
	private int[] _results = Array.Empty<int>();
	private int _purgeAt = PurgeThreshold;

	public VirtualRowList(AnnotationStore annotations, Func<int, bool> isSelected)
	{
		_annotations = annotations;
		_isSelected = isSelected;
	}

	public event NotifyCollectionChangedEventHandler? CollectionChanged;

	public LookupCounters Counters { get; } = new LookupCounters();

	/// <summary>
	/// The record numbers shown, in order.
	/// </summary>
	public int[] Results => _results;

	public int Count
	{
		get
		{
			this.Counters.Count++;
			return _results.Length;
		}
	}

	public bool IsReadOnly => true;

	public bool IsFixedSize => true;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	public object? this[int index]
	{
		get
		{
			this.Counters.Indexer++;
			return GetRow(index);
		}

		set => throw new NotSupportedException();
	}

	/// <summary>
	/// Shows new filter results. Rows created for the old results are no longer returned.
	/// </summary>
	public void Reset(int[] results)
	{
		_results = results;
		_rows.Clear();
		_purgeAt = PurgeThreshold;
		this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	/// <summary>
	/// Returns the row at a position, creating it if no live row exists. Not counted: the program's own lookups.
	/// </summary>
	public RowViewModel GetRow(int position)
	{
		if ((uint)position >= (uint)_results.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(position));
		}

		if (_rows.TryGetValue(position, out WeakReference<RowViewModel>? reference) && reference.TryGetTarget(out RowViewModel? existing))
		{
			return existing;
		}

		var recordNumber = _results[position];
		TimeSpan elapsed = position == 0
			? TimeSpan.MinValue
			: SyntheticLog.GetTimestamp(recordNumber) - SyntheticLog.GetTimestamp(_results[position - 1]);

		var row = new RowViewModel(position, recordNumber, elapsed, _isSelected(position), _annotations);
		this.Counters.RowsCreated++;

		_rows[position] = new WeakReference<RowViewModel>(row);
		if (_rows.Count >= _purgeAt)
		{
			PurgeDeadRows();
		}

		return row;
	}

	/// <summary>
	/// Returns the position of a record in the results, or -1. Not counted: the program's own lookups.
	/// </summary>
	public int FindPosition(int recordNumber)
	{
		var position = Array.BinarySearch(_results, recordNumber);
		return position >= 0 ? position : -1;
	}

	/// <summary>
	/// Returns the position of a line in the results, or -1: a binary search, as line numbers grow with record numbers.
	/// </summary>
	public int FindPositionOfLine(int lineNumber)
	{
		var low = 0;
		var high = _results.Length - 1;
		while (low <= high)
		{
			var middle = low + ((high - low) / 2);
			var line = SyntheticLog.GetLineNumber(_results[middle]);
			if (line == lineNumber)
			{
				return middle;
			}

			if (line < lineNumber)
			{
				low = middle + 1;
			}
			else
			{
				high = middle - 1;
			}
		}

		return -1;
	}

	public int IndexOf(object? value)
	{
		this.Counters.IndexOf++;
		return value is RowViewModel row ? FindPosition(row.RecordNumber) : -1;
	}

	public bool Contains(object? value)
	{
		this.Counters.Contains++;
		return value is RowViewModel row && FindPosition(row.RecordNumber) >= 0;
	}

	/// <summary>
	/// The rows still alive: the ones WPF holds, plus any the garbage collector has not freed yet.
	/// </summary>
	public IEnumerable<RowViewModel> GetLiveRows()
	{
		foreach (WeakReference<RowViewModel> reference in _rows.Values)
		{
			if (reference.TryGetTarget(out RowViewModel? row))
			{
				yield return row;
			}
		}
	}

	public int CountLiveRows()
	{
		PurgeDeadRows();
		return _rows.Count;
	}

	public IEnumerator GetEnumerator()
	{
		// WPF should never need this: walking 100 million rows would create every row.
		this.Counters.Enumerations++;
		for (var position = 0; position < _results.Length; position++)
		{
			yield return GetRow(position);
		}
	}

	public void CopyTo(Array array, int index)
	{
		this.Counters.CopyTo++;
		for (var position = 0; position < _results.Length; position++)
		{
			array.SetValue(GetRow(position), index + position);
		}
	}

	public int Add(object? value) => throw new NotSupportedException();

	public void Clear() => throw new NotSupportedException();

	public void Insert(int index, object? value) => throw new NotSupportedException();

	public void Remove(object? value) => throw new NotSupportedException();

	public void RemoveAt(int index) => throw new NotSupportedException();

	private void PurgeDeadRows()
	{
		var dead = _rows.Where(pair => !pair.Value.TryGetTarget(out _)).Select(pair => pair.Key).ToList();
		foreach (var position in dead)
		{
			_rows.Remove(position);
		}

		_purgeAt = System.Math.Max(PurgeThreshold, _rows.Count * 2);
	}
}
