namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Immutable;

	/// <summary>
	/// Records of a <see cref="RecordTable"/>, each created when it is read and not kept (#1140).
	/// </summary>
	/// <remarks>
	/// The list holds the table and, unless it lists every record in order, the numbers of the records it lists:
	/// 4 bytes per record instead of a record.
	/// </remarks>
	internal sealed class TableRecordList : RecordList
	{
		private readonly RecordTable _table;

		// Null when the list holds every record of the table, in order.
		private readonly int[] _recordNumbers;

		/// <summary>
		/// The table, when the list holds every one of its records in order, as after a full parse: otherwise <see langword="null"/>.
		/// </summary>
		/// <remarks>
		/// The saved index keeps this table, so a log can be reopened without parsing (#1023).
		/// </remarks>
		internal RecordTable WholeTable => _recordNumbers == null ? _table : null;

		public TableRecordList(RecordTable table)
			: this(table, null)
		{
			// nothing to do
		}

		private TableRecordList(RecordTable table, int[] recordNumbers)
		{
			_table = table ?? throw new ArgumentNullException(nameof(table));
			_recordNumbers = recordNumbers;
		}

		internal RecordTable Table => _table;

		/// <summary>
		/// The list holds every record of its table, in order, so a position is a record number.
		/// </summary>
		internal bool ListsWholeTable => _recordNumbers == null;

		public override int Count => _recordNumbers?.Length ?? _table.Count;

		public override IRecord this[int position] => _table.CreateRecord(GetRecordNumber(position));

		public override int GetLineNumber(int position) => _table.GetLineNumber(GetRecordNumber(position));

		public override DateTime GetCreatedAt(int position) => _table.GetCreatedAt(GetRecordNumber(position));

		public override ReadOnlySpan<char> ReadContent(int position, ContentBuffer content)
		{
			ArgumentNullException.ThrowIfNull(content);

			return _table.ReadContent(GetRecordNumber(position), content);
		}

		public override RecordFacts GetFacts(int position) => _table.GetFacts(GetRecordNumber(position));

		/// <summary>
		/// Returns the records at <paramref name="positions"/> of <paramref name="records"/>, in that order.
		/// </summary>
		/// <param name="positions">Positions in <paramref name="records"/>. Over a table, the array becomes the new list's record numbers.</param>
		/// <remarks>
		/// Over a table, the result is a list over the same table, so no record is created. Otherwise, as in tests, it holds the records.
		/// </remarks>
		public static RecordList Select(RecordList records, int[] positions)
		{
			if (positions.Length == 0)
			{
				return RecordList.Empty;
			}

			return records is TableRecordList list
				? new TableRecordList(list._table, list.ToRecordNumbers(positions))
				: CopyRecords(records, positions);
		}

		/// <summary>
		/// Returns the records at <paramref name="positions"/> of <paramref name="records"/>, in that order, over a new table of only those records.
		/// </summary>
		/// <param name="positions">Positions in <paramref name="records"/>, which the method may change.</param>
		/// <remarks>
		/// Clear keeps one, so the records it clears, and the chunks that no kept record is read from, can be freed.
		/// </remarks>
		public static RecordList Keep(RecordList records, int[] positions)
		{
			if (positions.Length == 0)
			{
				return RecordList.Empty;
			}

			return records is TableRecordList list
				? new TableRecordList(list._table.Keep(list.ToRecordNumbers(positions)))
				: CopyRecords(records, positions);
		}

		/// <summary>
		/// Turns positions in this list into the record numbers they refer to, in the same array.
		/// </summary>
		private int[] ToRecordNumbers(int[] positions)
		{
			if (_recordNumbers != null)
			{
				for (var index = 0; index < positions.Length; index++)
				{
					positions[index] = _recordNumbers[positions[index]];
				}
			}

			return positions;
		}

		/// <summary>
		/// A list that is not over a table, as tests build, keeps the records themselves.
		/// </summary>
		private static RecordList CopyRecords(RecordList records, int[] positions)
		{
			ImmutableArray<IRecord>.Builder selected = ImmutableArray.CreateBuilder<IRecord>(positions.Length);

			foreach (var position in positions)
			{
				selected.Add(records[position]);
			}

			return selected.MoveToImmutable();
		}

		private int GetRecordNumber(int position)
		{
			// Without record numbers, a position is a record number, which the table checks.
			return _recordNumbers != null ? _recordNumbers[position] : position;
		}
	}
}
