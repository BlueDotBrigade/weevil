namespace BlueDotBrigade.Weevil.IO
{
	/// <summary>
	/// Where an opened log's content is kept (#421, #1025).
	/// </summary>
	public enum StorageMode
	{
		/// <summary>
		/// Each record's content is kept in memory until the log is reloaded.
		/// </summary>
		Ram = 0,

		/// <summary>
		/// Only the record table stays in memory: content is read back from the log whenever it is needed.
		/// </summary>
		Disk = 1,
	}
}
