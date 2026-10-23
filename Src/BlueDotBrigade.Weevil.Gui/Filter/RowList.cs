namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Threading;

	/// <summary>
	/// The filter results as the grid sees them: a row view-model is created only for a row that WPF asks for (#1015).
	/// </summary>
	/// <remarks>
	/// It implements the non-generic <see cref="IList"/>, so WPF reads through it instead of copying every item.
	/// A row is cached while something holds it, so WPF gets the same instance back, but the cache never keeps a row alive.
	/// Each filter creates a new list.
	/// </remarks>
	internal sealed class RowList : IList
	{
		// More changes than this before the rows refresh, and every live row refreshes once instead.
		private const int MaximumQueuedChanges = 1_000;
		private const int MinimumPurgeCount = 4_096;

		private readonly ImmutableArray<IRecord> _results;
		private readonly IAnnotations _annotations;
		private readonly ISelect _selector;
		private readonly IUiDispatcher _uiDispatcher;

		// Guards the cache and the queued changes. Rows are read on the UI thread, but changes arrive on any thread.
		private readonly object _padlock = new object();
		private readonly Dictionary<int, WeakReference<RowViewModel>> _rows = new Dictionary<int, WeakReference<RowViewModel>>();
		private int _purgeCount = MinimumPurgeCount;

		private List<AnnotationChangedEventArgs> _queuedChanges = new List<AnnotationChangedEventArgs>();
		private bool _isRefreshQueued;
		private bool _refreshesEveryRow;

		public RowList(ImmutableArray<IRecord> results, IAnnotations annotations, ISelect selector, IUiDispatcher uiDispatcher)
		{
			_results = results.IsDefault ? ImmutableArray<IRecord>.Empty : results;
			_annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
			_selector = selector ?? throw new ArgumentNullException(nameof(selector));
			_uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
		}

		/// <summary>
		/// Raised by <see cref="RefreshSelection"/> with the rows still held whose selection changed.
		/// </summary>
		public event EventHandler<IReadOnlyList<RowViewModel>> SelectionRefreshed;

		public int Count => _results.Length;

		/// <summary>
		/// The number of selected records.
		/// </summary>
		public int SelectedCount => _selector.Selected.Count;

		public bool IsReadOnly => true;

		public bool IsFixedSize => true;

		public bool IsSynchronized => false;

		public object SyncRoot => this;

		public object this[int index]
		{
			get => GetRow(index);
			set => throw new NotSupportedException();
		}

		public RowViewModel GetRow(int position)
		{
			if ((uint)position >= (uint)_results.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(position), position, "The position is outside the filter results.");
			}

			lock (_padlock)
			{
				if (_rows.TryGetValue(position, out WeakReference<RowViewModel> reference) && reference.TryGetTarget(out RowViewModel row))
				{
					return row;
				}

				row = new RowViewModel(_results[position], _annotations)
				{
					IsSelected = _selector.Selected.ContainsKey(_results[position].LineNumber),
				};
				_rows[position] = new WeakReference<RowViewModel>(row);

				if (_rows.Count >= _purgeCount)
				{
					RemoveDeadRows();
				}

				return row;
			}
		}

		public int LineNumberAt(int position)
		{
			return _results[position].LineNumber;
		}

		/// <summary>
		/// Returns the position of the record with the given line number, or -1 when the filter hides it.
		/// </summary>
		public int IndexOfLineNumber(int lineNumber)
		{
			var position = IndexOfLineNumberOrNext(lineNumber);

			return position < _results.Length && _results[position].LineNumber == lineNumber ? position : -1;
		}

		/// <summary>
		/// Returns the position of the first record at or after the given line number, or <see cref="Count"/> when there is none.
		/// </summary>
		public int IndexOfLineNumberOrNext(int lineNumber)
		{
			var low = 0;
			var high = _results.Length;

			while (low < high)
			{
				var middle = low + ((high - low) / 2);

				if (_results[middle].LineNumber < lineNumber)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			return low;
		}

		/// <summary>
		/// Shows the current selection on the rows still held. Call it on the UI thread after the selection changes.
		/// </summary>
		public void RefreshSelection()
		{
			IDictionary<int, IRecord> selected = _selector.Selected;
			var liveRows = new List<RowViewModel>();

			lock (_padlock)
			{
				foreach (WeakReference<RowViewModel> reference in _rows.Values)
				{
					if (reference.TryGetTarget(out RowViewModel row))
					{
						liveRows.Add(row);
					}
				}
			}

			var changedRows = new List<RowViewModel>();

			foreach (RowViewModel row in liveRows)
			{
				var isSelected = selected.ContainsKey(row.LineNumber);

				if (row.IsSelected != isSelected)
				{
					row.IsSelected = isSelected;
					changedRows.Add(row);
				}
			}

			if (changedRows.Count > 0)
			{
				this.SelectionRefreshed?.Invoke(this, changedRows);
			}
		}

		/// <summary>
		/// Returns the positions of the selected rows after <paramref name="afterPosition"/>, in order, without creating a row for each.
		/// </summary>
		/// <param name="afterPosition">The position to start after, or -1 to start at the first row.</param>
		public IEnumerable<int> GetSelectedPositions(int afterPosition)
		{
			var afterLineNumber = afterPosition >= 0 ? _results[afterPosition].LineNumber : int.MinValue;

			foreach (var lineNumber in _selector.Selected.Keys)
			{
				if (lineNumber > afterLineNumber)
				{
					var position = IndexOfLineNumber(lineNumber);

					if (position >= 0)
					{
						yield return position;
					}
				}
			}
		}

		public int IndexOf(object value)
		{
			return value is RowViewModel row ? IndexOfLineNumber(row.LineNumber) : -1;
		}

		public bool Contains(object value)
		{
			return IndexOf(value) >= 0;
		}

		/// <summary>
		/// Refreshes the row of the changed line later, on the UI thread, if it is on screen (decision 4).
		/// </summary>
		/// <remarks>
		/// Called on the thread that changed the annotation, so it never waits for the UI thread.
		/// </remarks>
		public void QueueRefresh(AnnotationChangedEventArgs change)
		{
			var isFirstChange = false;

			lock (_padlock)
			{
				if (!_refreshesEveryRow)
				{
					if (_queuedChanges.Count < MaximumQueuedChanges)
					{
						_queuedChanges.Add(change);
					}
					else
					{
						_refreshesEveryRow = true;
						_queuedChanges.Clear();
					}
				}

				if (!_isRefreshQueued)
				{
					_isRefreshQueued = true;
					isFirstChange = true;
				}
			}

			if (isFirstChange)
			{
				_uiDispatcher.BeginInvoke(RefreshChangedRows, DispatcherPriority.Background);
			}
		}

		/// <remarks>
		/// WPF should not need this: walking the list creates a row for every result.
		/// </remarks>
		public IEnumerator GetEnumerator()
		{
			for (var position = 0; position < _results.Length; position++)
			{
				yield return GetRow(position);
			}
		}

		public void CopyTo(Array array, int index)
		{
			for (var position = 0; position < _results.Length; position++)
			{
				array.SetValue(GetRow(position), index + position);
			}
		}

		public int Add(object value) => throw new NotSupportedException();

		public void Clear() => throw new NotSupportedException();

		public void Insert(int index, object value) => throw new NotSupportedException();

		public void Remove(object value) => throw new NotSupportedException();

		public void RemoveAt(int index) => throw new NotSupportedException();

		private void RefreshChangedRows()
		{
			List<AnnotationChangedEventArgs> changes;
			bool refreshesEveryRow;
			var liveRows = new Dictionary<int, RowViewModel>();

			lock (_padlock)
			{
				changes = _queuedChanges;
				refreshesEveryRow = _refreshesEveryRow;

				_queuedChanges = new List<AnnotationChangedEventArgs>();
				_refreshesEveryRow = false;
				_isRefreshQueued = false;

				foreach (WeakReference<RowViewModel> reference in _rows.Values)
				{
					if (reference.TryGetTarget(out RowViewModel row))
					{
						liveRows[row.LineNumber] = row;
					}
				}
			}

			// Outside the lock: the bindings read the store while handling PropertyChanged.
			if (refreshesEveryRow)
			{
				foreach (RowViewModel row in liveRows.Values)
				{
					row.Refresh(source: null);
				}
			}
			else
			{
				foreach (AnnotationChangedEventArgs change in changes)
				{
					if (liveRows.TryGetValue(change.LineNumber, out RowViewModel row))
					{
						row.Refresh(change.Source);
					}
				}
			}
		}

		// Called while holding the lock.
		private void RemoveDeadRows()
		{
			var deadPositions = new List<int>();

			foreach (KeyValuePair<int, WeakReference<RowViewModel>> pair in _rows)
			{
				if (!pair.Value.TryGetTarget(out _))
				{
					deadPositions.Add(pair.Key);
				}
			}

			foreach (var position in deadPositions)
			{
				_rows.Remove(position);
			}

			_purgeCount = System.Math.Max(MinimumPurgeCount, _rows.Count * 2);
		}
	}
}
