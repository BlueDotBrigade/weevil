namespace BlueDotBrigade.Weevil.Filter
{
	using BlueDotBrigade.Weevil.Data;

	internal class FilterMapReduceState
	{
		public FilterMapReduceState()
		{
			this.Count = 0;
		}
		/// <summary>
		/// Represents the number of records that meet the search criteria.
		/// </summary>
		internal int Count { get; set; }

		/// <summary>
		/// Holds the content of the record this thread is filtering, so it is read once for every expression (#1131).
		/// </summary>
		internal ContentBuffer Content { get; } = new ContentBuffer();
	}
}