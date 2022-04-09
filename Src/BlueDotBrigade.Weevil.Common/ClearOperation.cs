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
	}
}
