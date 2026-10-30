namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Immutable;

	/// <summary>
	/// A read-only list of records, in the order they were given, which callers read instead of an <see cref="ImmutableArray{T}"/> (#1136).
	/// </summary>
	/// <remarks>
	/// Once records are created when they are read (#1021), a list may create each record it returns, so read a record once and keep it,
	/// rather than reading the same position many times. A list never changes: filters and Clear publish new lists.
	/// <see cref="IndexOf"/> and <see cref="Contains"/> find a record by its line number, as records are equal by line number (#1135).
	/// </remarks>
	public abstract class RecordList : IList<IRecord>, IReadOnlyList<IRecord>
	{
		/// <summary>
		/// A list without records.
		/// </summary>
		public static readonly RecordList Empty = new ArrayRecordList(ImmutableArray<IRecord>.Empty);

		public abstract int Count { get; }

		/// <summary>
		/// The same as <see cref="Count"/>, for callers written for <see cref="ImmutableArray{T}"/>. Removed by #1141.
		/// </summary>
		public int Length => this.Count;

		public bool IsEmpty => this.Count == 0;

		public bool IsReadOnly => true;

		public abstract IRecord this[int position] { get; }

		IRecord IList<IRecord>.this[int index]
		{
			get => this[index];
			set => throw new NotSupportedException("A record list cannot be changed.");
		}

		/// <summary>
		/// Wraps <paramref name="records"/> without copying them. A default array becomes <see cref="Empty"/>.
		/// </summary>
		public static implicit operator RecordList(ImmutableArray<IRecord> records)
		{
			return records.IsDefaultOrEmpty ? Empty : new ArrayRecordList(records);
		}

		/// <summary>
		/// Returns the position of the record on the same line as <paramref name="record"/>, or -1.
		/// </summary>
		public virtual int IndexOf(IRecord record)
		{
			if (record is null)
			{
				return -1;
			}

			for (var position = 0; position < this.Count; position++)
			{
				if (this[position].LineNumber == record.LineNumber)
				{
					return position;
				}
			}

			return -1;
		}

		public bool Contains(IRecord record)
		{
			return IndexOf(record) >= 0;
		}

		public void CopyTo(IRecord[] array, int arrayIndex)
		{
			ArgumentNullException.ThrowIfNull(array);

			for (var position = 0; position < this.Count; position++)
			{
				array[arrayIndex + position] = this[position];
			}
		}

		/// <summary>
		/// Returns the records as an <see cref="ImmutableArray{T}"/>, without copying them when the list already holds one.
		/// </summary>
		public virtual ImmutableArray<IRecord> ToImmutableArray()
		{
			ImmutableArray<IRecord>.Builder records = ImmutableArray.CreateBuilder<IRecord>(this.Count);

			foreach (IRecord record in this)
			{
				records.Add(record);
			}

			return records.MoveToImmutable();
		}

		public virtual IEnumerator<IRecord> GetEnumerator()
		{
			for (var position = 0; position < this.Count; position++)
			{
				yield return this[position];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		void ICollection<IRecord>.Add(IRecord item) => throw new NotSupportedException("A record list cannot be changed.");

		void ICollection<IRecord>.Clear() => throw new NotSupportedException("A record list cannot be changed.");

		bool ICollection<IRecord>.Remove(IRecord item) => throw new NotSupportedException("A record list cannot be changed.");

		void IList<IRecord>.Insert(int index, IRecord item) => throw new NotSupportedException("A record list cannot be changed.");

		void IList<IRecord>.RemoveAt(int index) => throw new NotSupportedException("A record list cannot be changed.");

		/// <summary>
		/// A record list over an array of records that already exist.
		/// </summary>
		private sealed class ArrayRecordList : RecordList
		{
#if DEBUG
			// Set WEEVIL_TEST_RECORD_COPIES=1 to return a new copy of a record on every read, as a list over a record table will (#1021),
			// so a test run shows which code still relies on reading the same record object twice (#1136).
			private static readonly bool IsCopyingOnRead = Environment.GetEnvironmentVariable("WEEVIL_TEST_RECORD_COPIES") == "1";
#endif

			private readonly ImmutableArray<IRecord> _records;

			public ArrayRecordList(ImmutableArray<IRecord> records)
			{
				_records = records;
			}

			public override int Count => _records.Length;

			public override IRecord this[int position]
			{
				get
				{
#if DEBUG
					if (IsCopyingOnRead && _records[position] is Record record)
					{
						return record.Copy();
					}
#endif
					return _records[position];
				}
			}

			public override ImmutableArray<IRecord> ToImmutableArray()
			{
#if DEBUG
				if (IsCopyingOnRead)
				{
					return base.ToImmutableArray();
				}
#endif
				return _records;
			}
		}
	}
}
