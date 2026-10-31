namespace BlueDotBrigade.Weevil.Data
{
	using System;

	/// <summary>
	/// The records at chosen positions of another record list, in the chosen order, so reordering a list keeps 4 bytes per record
	/// instead of a copy of every record (#1137).
	/// </summary>
	internal sealed class PositionRecordList : RecordList
	{
		private readonly RecordList _records;
		private readonly int[] _positions;

		/// <param name="records">The list the positions refer to.</param>
		/// <param name="positions">Positions in <paramref name="records"/>, in the order this list returns them.</param>
		public PositionRecordList(RecordList records, int[] positions)
		{
			_records = records ?? throw new ArgumentNullException(nameof(records));
			_positions = positions ?? throw new ArgumentNullException(nameof(positions));
		}

		public override int Count => _positions.Length;

		public override IRecord this[int position] => _records[_positions[position]];

		public override int GetLineNumber(int position) => _records.GetLineNumber(_positions[position]);

		public override DateTime GetCreatedAt(int position) => _records.GetCreatedAt(_positions[position]);
	}
}
