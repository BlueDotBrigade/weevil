namespace BlueDotBrigade.Weevil.Diagnostics
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
		/// Distribution source associated with the running Weevil instance.
		/// </summary>
		/// <remarks>
		/// Expected values: Development, CiPipeline, Production
		/// </remarks>
		public string Source { get; set; } = "Development";

		/// <summary>
		/// Application version for the session.
		/// </summary>
		public Version Version { get; set; } = new Version(0, 0);

		/// <summary>
		/// Indicates whether a debugger was attached when the application started.
		/// </summary>
		public bool IsDebugging { get; set; }

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
		/// Installed CPU model name reported by the operating system.
		/// </summary>
		public string InstalledCpu { get; set; } = "Unknown";

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
		/// Number of times help was opened in the session.
		/// </summary>
		public int HelpOpenCount { get; set; }

		/// <summary>
		/// Telemetry schema version for the payload.
		/// </summary>
		public string SchemaVersion { get; set; } = string.Empty;
	}
}