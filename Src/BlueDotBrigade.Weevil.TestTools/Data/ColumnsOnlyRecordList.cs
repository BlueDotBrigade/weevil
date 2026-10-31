namespace BlueDotBrigade.Weevil.TestTools.Data
{
	using System;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// A record list that reads line numbers and timestamps from another list, but throws when a record is read,
	/// so a test shows that code which needs only those creates no record (#1137).
	/// </summary>
	public sealed class ColumnsOnlyRecordList : RecordList
	{
		private readonly RecordList _records;

		public ColumnsOnlyRecordList(RecordList records)
		{
			_records = records ?? throw new ArgumentNullException(nameof(records));
		}

		public override int Count => _records.Count;

		public override IRecord this[int position] => throw new InvalidOperationException($"The record at position {position} was read, which creates it.");

		public override int GetLineNumber(int position) => _records.GetLineNumber(position);

		public override DateTime GetCreatedAt(int position) => _records.GetCreatedAt(position);
	}
}
