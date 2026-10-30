namespace BlueDotBrigade.Weevil.Data
{
	using System.Threading;

	/// <summary>
	/// The records a view keeps in memory, in line number order (#1018). Clear narrows it; the engine, its filter and its selection read it.
	/// </summary>
	/// <remarks>
	/// Clear replaces the whole list in one write, so a reader sees either the records before the Clear or those after it.
	/// A reader that uses the records more than once reads this property once.
	/// </remarks>
	internal sealed class RecordScope
	{
		private RecordList _records;

		public RecordScope(RecordList records)
		{
			_records = records;
		}

		public RecordList Records => _records;

		/// <param name="records">The records kept: a subset of <see cref="Records"/>, in line number order.</param>
		public void Narrow(RecordList records)
		{
			Interlocked.Exchange(ref _records, records);
		}
	}
}
