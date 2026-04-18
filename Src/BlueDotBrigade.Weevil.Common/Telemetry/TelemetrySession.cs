namespace BlueDotBrigade.Weevil
{
	using System;

	/// <summary>
	/// Represents the session-level telemetry payload for a single application session.
	/// </summary>
	public class TelemetrySession
	{
		/// <summary>
		/// Unique identifier of the session.
		/// </summary>
		public Guid SessionId { get; set; }

		/// <summary>
		/// Executable name associated with the session (for example, WeevilGui.exe or WeevilCli.exe).
		/// </summary>
		public string Application { get; set; } = string.Empty;

		/// <summary>
		/// Application version for the session.
		/// </summary>
		public string Version { get; set; } = string.Empty;

		/// <summary>
		/// UTC timestamp when the session began.
		/// </summary>
		public DateTime SessionStartUtc { get; set; }

		/// <summary>
		/// UTC timestamp when the session ended.
		/// </summary>
		public DateTime SessionEndUtc { get; set; }

		/// <summary>
		/// Active session duration in minutes.
		/// </summary>
		public double SessionActiveMinutes { get; set; }

		/// <summary>
		/// Size of the opened log file in bytes.
		/// </summary>
		public long LogFileSizeBytes { get; set; }

		/// <summary>
		/// Installed machine memory in megabytes.
		/// </summary>
		public long InstalledRamMb { get; set; }

		/// <summary>
		/// Number of filter executions in the session.
		/// </summary>
		public int FilterExecutionCount { get; set; }

		/// <summary>
		/// Number of times the graph view was opened in the session.
		/// </summary>
		public int GraphOpenCount { get; set; }

		/// <summary>
		/// Number of times the dashboard was opened in the session.
		/// </summary>
		public int DashboardOpenCount { get; set; }

		/// <summary>
		/// Telemetry schema version for the payload.
		/// </summary>
		public string SchemaVersion { get; set; } = string.Empty;
	}
}
