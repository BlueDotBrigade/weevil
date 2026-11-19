namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;

	/// <summary>
	/// A log on disk can no longer be read: it was renamed, deleted, replaced, truncated or corrupted since it was parsed (#1025).
	/// </summary>
	/// <remarks>
	/// Every read of a record's text from a log on disk that fails throws it, with what the read threw inside, so a caller catches one type (#1295).
	/// <see cref="IsUnreadable"/> tells a log that is gone from one whose bytes changed, which Reload reads again.
	/// </remarks>
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

		private LogFileChangedException(string message, string path, Exception readFailure)
			: base(message, readFailure)
		{
			this.Path = path;
			this.IsUnreadable = true;
		}

		public string Path { get; } = string.Empty;

		/// <summary>
		/// The log can no longer be opened at <see cref="Path"/>, for example because it was renamed or deleted, so Reload cannot read it either.
		/// Otherwise its bytes changed, and Reload reads the latest.
		/// </summary>
		/// <remarks>
		/// <see cref="Exception.InnerException"/> holds what the read threw.
		/// </remarks>
		public bool IsUnreadable { get; }

		/// <summary>
		/// The log can no longer be opened at <paramref name="path"/>, for example because it was renamed or deleted: <paramref name="readFailure"/> says why.
		/// </summary>
		internal static LogFileChangedException FromReadFailure(string path, Exception readFailure)
		{
			return new LogFileChangedException(
				$"The log can no longer be read. It may have been renamed or deleted. Path={path}, Reason=`{readFailure.Message}`",
				path,
				readFailure);
		}

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
