namespace BlueDotBrigade.Weevil.Filter
{
	using BlueDotBrigade.Weevil.Data;

	internal class FilterMapReduceState
	{
		public FilterMapReduceState(ContentBuffer content)
		{
			this.Count = 0;
			this.Content = content;
		}
		/// <summary>
		/// Represents the number of records that meet the search criteria.
		/// </summary>
		internal int Count { get; set; }

		/// <summary>
		/// Holds the content of the record being filtered, so it is read once, however many expressions test it (#1131).
		/// </summary>
		internal ContentBuffer Content { get; }
	}
}