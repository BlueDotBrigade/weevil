namespace BlueDotBrigade.Weevil
{
	using System;

	public class TelemetrySession
	{
		public Guid SessionId { get; set; }
		public string Application { get; set; } = string.Empty;
		public string Version { get; set; } = string.Empty;
		public DateTime SessionStartUtc { get; set; }
		public DateTime SessionEndUtc { get; set; }
		public double SessionActiveMinutes { get; set; }
		public long LogFileSizeBytes { get; set; }
		public long InstalledRamMb { get; set; }
		public int FilterExecutionCount { get; set; }
		public int GraphOpenCount { get; set; }
		public int DashboardOpenCount { get; set; }
		public string SchemaVersion { get; set; } = string.Empty;
	}
}
