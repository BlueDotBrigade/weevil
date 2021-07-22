namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Immutable;

	public static class RecordExtensions
	{
		/// <summary>
		/// Determines the index value of the <paramref name="record"/> within the provided collection.
		/// </summary>
		/// <param name="record"></param>
		/// <param name="records"></param>
		/// <returns></returns>
		public static int ToIndexUsing(this IRecord record, ImmutableArray<IRecord> records)
		{
			var index = records.BinarySearch(new Record(record.LineNumber), new RecordLineNumberComparer());
			return index;
		}
	}
}
