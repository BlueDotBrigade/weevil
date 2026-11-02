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

		public override int Count => _recordNumbers?.Length ?? _table.Count;

		public override IRecord this[int position] => _table.CreateRecord(GetRecordNumber(position));

		public override int GetLineNumber(int position) => _table.GetLineNumber(GetRecordNumber(position));

		public override DateTime GetCreatedAt(int position) => _table.GetCreatedAt(GetRecordNumber(position));

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

			if (records is TableRecordList list)
			{
				if (list._recordNumbers != null)
				{
					for (var index = 0; index < positions.Length; index++)
					{
						positions[index] = list._recordNumbers[positions[index]];
					}
				}

				return new TableRecordList(list._table, positions);
			}

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
