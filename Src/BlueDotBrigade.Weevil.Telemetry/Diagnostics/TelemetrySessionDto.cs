namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;

	/// <summary>
	/// XML-friendly telemetry session payload stored in the local outbox.
	/// </summary>
	public sealed class TelemetrySessionDto
	{
		public Guid SessionId { get; set; }

		public string Application { get; set; } = string.Empty;

		public string Source { get; set; } = string.Empty;

		public string Version { get; set; } = "0.0";

		public bool IsDebugging { get; set; }

		public DateTime SessionStartUtc { get; set; }

		public DateTime SessionEndUtc { get; set; }

		public double SessionActiveMinutes { get; set; }

		public long LogFileSizeBytes { get; set; }

		public long InstalledRamMb { get; set; }

		public string InstalledCpu { get; set; } = string.Empty;

		public int FilterExecutionCount { get; set; }

		public int GraphOpenCount { get; set; }

		public int DashboardOpenCount { get; set; }

		public string SchemaVersion { get; set; } = "2.0";
	}
}
