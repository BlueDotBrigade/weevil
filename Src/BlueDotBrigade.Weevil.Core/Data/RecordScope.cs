namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Immutable;

	/// <summary>
	/// The records a view keeps in memory, in line number order (#1018). Clear narrows it; the engine, its filter and its selection read it.
	/// </summary>
	/// <remarks>
	/// Clear replaces the whole array in one write, so a reader sees either the records before the Clear or those after it.
	/// A reader that uses the records more than once reads this property once.
	/// </remarks>
	internal sealed class RecordScope
	{
		private ImmutableArray<IRecord> _records;

		public RecordScope(ImmutableArray<IRecord> records)
		{
			_records = records;
		}

		public ImmutableArray<IRecord> Records => _records;

		/// <param name="records">The records kept: a subset of <see cref="Records"/>, in line number order.</param>
		public void Narrow(ImmutableArray<IRecord> records)
		{
			ImmutableInterlocked.InterlockedExchange(ref _records, records);
		}
	}
}
