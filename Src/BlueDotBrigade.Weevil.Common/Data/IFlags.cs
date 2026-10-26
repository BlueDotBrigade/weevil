namespace BlueDotBrigade.Weevil.Data
{
	/// <summary>
	/// The records an analyzer flagged in one view of a log, kept by line number (decision 2 of the #990 plan).
	/// </summary>
	public interface IFlags
	{
		bool IsFlagged(int lineNumber);

		void SetFlagged(int lineNumber, bool isFlagged);
	}
}
