namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;

	/// <summary>
	/// The values recently used in one of the filter's fields, newest first (#1109).
	/// </summary>
	/// <remarks>
	/// Writes are serialized by the session's lock (#1107). Each write publishes a new array, so reads never wait.
	/// The object stays the same while its values change, so a <see cref="IFilterBehaviors.HistoryChanged"/> sender can be compared with it.
	/// </remarks>
	[DebuggerDisplay("Count={_values.Length}")]
	internal sealed class FilterHistory : IReadOnlyList<string>
	{
		private readonly object _writeLock;
		private ImmutableArray<string> _values = ImmutableArray<string>.Empty;

		internal FilterHistory() : this(new object())
		{
			// nothing to do
		}

		/// <param name="writeLock">The lock that serializes writes, shared by every store of the session.</param>
		internal FilterHistory(object writeLock)
		{
			_writeLock = writeLock ?? throw new ArgumentNullException(nameof(writeLock));
		}

		public int Count => _values.Length;

		public string this[int index] => _values[index];

		public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_values).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Replaces every value, such as with those read from the sidecar.
		/// </summary>
		internal void Load(IEnumerable<string> values)
		{
			lock (_writeLock)
			{
				Publish(values?.ToImmutableArray() ?? ImmutableArray<string>.Empty);
			}
		}

		/// <summary>
		/// Puts the value first: moves it there if the history holds it, or adds it, dropping the oldest value when the history is full.
		/// </summary>
		/// <returns>The changes, in the order they were made.</returns>
		internal IReadOnlyList<HistoryChangedEventArgs> Add(string value)
		{
			lock (_writeLock)
			{
				ImmutableArray<string> values = _values;
				var index = values.IndexOf(value);

				if (index >= 0)
				{
					Publish(values.RemoveAt(index).Insert(0, value));
					return [new HistoryChangedEventArgs(HistoryChangeType.Moved, index, value)];
				}

				var changes = new List<HistoryChangedEventArgs>(2);

				if (values.Length >= FilterManager.MaxFilterHistory)
				{
					var oldest = values.Length - 1;
					changes.Add(new HistoryChangedEventArgs(HistoryChangeType.Removed, oldest, values[oldest]));
					values = values.RemoveAt(oldest);
				}

				Publish(values.Insert(0, value));
				changes.Add(new HistoryChangedEventArgs(HistoryChangeType.Added, 0, value));

				return changes;
			}
		}

		// Called while holding the lock.
		private void Publish(ImmutableArray<string> values)
		{
			ImmutableInterlocked.InterlockedExchange(ref _values, values);
		}
	}
}
