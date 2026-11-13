namespace BlueDotBrigade.Weevil.Data
{
	/// <summary>
	/// Finds where each record starts in a log, and what it holds.
	/// </summary>
	/// <remarks>
	/// What a parser returns must depend only on the log's lines: not on the culture, the time zone, the clock, settings or the log's path.
	/// A log opened before is reopened from the record table saved with its index, without calling the parser (#1023),
	/// so a parser that depended on anything else would show different records on a reopen.
	/// </remarks>
	public interface IRecordParser
	{
		bool TryParse(int line, string content, out IRecord record);
	}
}