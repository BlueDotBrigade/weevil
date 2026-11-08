namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;

	/// <summary>
	/// The log was truncated, replaced or corrupted since it was parsed, so its bytes can no longer be read back (#1025).
	/// </summary>
	public sealed class LogFileChangedException : IOException
	{
		public LogFileChangedException()
			: base("The log has changed since it was opened.")
		{
		}

		public LogFileChangedException(string message)
			: base(message)
		{
		}

		public LogFileChangedException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// The log is shorter than the range parsed.
		/// </summary>
		public LogFileChangedException(string path, long expectedLength, long actualLength)
			: base($"The log is shorter than when it was opened. Path={path}, ExpectedLength={expectedLength}, ActualLength={actualLength}")
		{
			this.Path = path;
			this.ExpectedLength = expectedLength;
			this.ActualLength = actualLength;
		}

		/// <summary>
		/// The block at <paramref name="changedOffset"/> has other bytes than when it was parsed.
		/// </summary>
		public LogFileChangedException(string path, long changedOffset)
			: base($"The log has changed since it was opened. Path={path}, ChangedOffset={changedOffset}")
		{
			this.Path = path;
			this.ChangedOffset = changedOffset;
		}

		public string Path { get; } = string.Empty;

		/// <summary>
		/// The length the log was expected to have at least, when it is shorter: otherwise -1.
		/// </summary>
		public long ExpectedLength { get; } = -1;

		/// <summary>
		/// The length the log was found to have, when it is shorter: otherwise -1.
		/// </summary>
		public long ActualLength { get; } = -1;

		/// <summary>
		/// Where the block whose bytes changed starts in the file, or where the range parsed starts within it: otherwise -1.
		/// </summary>
		public long ChangedOffset { get; } = -1;
	}
}
