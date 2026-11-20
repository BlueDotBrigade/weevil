namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;

	/// <summary>
	/// Another instance of Weevil has the log open, so it is not opened: the two would overwrite each other's sidecar and saved index (#1254).
	/// </summary>
	/// <remarks>
	/// The message names who has it, for example <c>app.log is already open in Weevil by jsmith on PC-42.</c>,
	/// so a caller shows it as it is.
	/// </remarks>
	public sealed class LogInUseException : IOException
	{
		public LogInUseException()
			: base("The log is already open in another Weevil.")
		{
		}

		public LogInUseException(string message)
			: base(message)
		{
		}

		public LogInUseException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <param name="logPath">The log that was not opened.</param>
		/// <param name="userName">The user who has it open, or <see langword="null"/> when that can't be read.</param>
		/// <param name="computerName">The computer it is open on, or <see langword="null"/> when that can't be read.</param>
		/// <param name="innerException">Why the log could not be locked.</param>
		public LogInUseException(string logPath, string userName, string computerName, Exception innerException)
			: base(GetMessage(logPath, userName, computerName), innerException)
		{
			this.LogPath = logPath;
			this.UserName = userName;
			this.ComputerName = computerName;
		}

		/// <summary>
		/// The log that was not opened, or <see langword="null"/> when the exception was created without one.
		/// </summary>
		public string LogPath { get; }

		/// <summary>
		/// The user who has the log open, or <see langword="null"/> when that can't be read, for example while the other Weevil is still writing it.
		/// </summary>
		public string UserName { get; }

		/// <summary>
		/// The computer the log is open on, or <see langword="null"/> when that can't be read.
		/// </summary>
		public string ComputerName { get; }

		private static string GetMessage(string logPath, string userName, string computerName)
		{
			var logName = Path.GetFileName(logPath);

			return string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(computerName)
				? $"{logName} is already open in another Weevil."
				: $"{logName} is already open in Weevil by {userName} on {computerName}.";
		}
	}
}
