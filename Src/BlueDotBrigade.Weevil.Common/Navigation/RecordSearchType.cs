namespace BlueDotBrigade.Weevil.Navigation
{
	/// <summary>
	/// Indicates what is considered an acceptable result.
	/// </summary>
	public enum RecordSearchType
	{
		/// <summary>
		/// Finds the record with the requested line number.
		/// </summary>
		ExactMatch,

		/// <summary>
		/// Finds the record that is closest to the requested line number.
		/// </summary>
		NearestNeighbor,

		ExactOrNext,

		ExactOrPrevious,
	}
}
