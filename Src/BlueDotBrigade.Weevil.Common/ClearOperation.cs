namespace BlueDotBrigade.Weevil
{
	/// <summary>
	/// Indicates which records should be removed from memory (RAM).
	/// </summary>
	public enum ClearOperation
	{
		BeforeSelected,
		BeforeAndAfterSelected,
		BetweenSelected,
		AfterSelected,
		Selected,
		Unselected,
		/// <summary>
		/// Records outside of the region of interest are cleared from memory.
		/// </summary>
		BeyondRegions,
	}
}
