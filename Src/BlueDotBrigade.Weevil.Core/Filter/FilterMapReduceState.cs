namespace BlueDotBrigade.Weevil.Filter
{
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
	}
}