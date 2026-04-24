namespace BlueDotBrigade.Weevil.Telemetry.MsSql
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.ComponentModel.DataAnnotations.Schema;

	/// <summary>
	/// Entity Framework entity that maps a <see cref="TelemetrySession"/> to
	/// the <c>telemetry.Session</c> table in Azure SQL.
	/// </summary>
	[Table("Session", Schema = "telemetry")]
	internal sealed class SessionRecord
	{
		[Key]
		public Guid SessionId { get; set; }

		[Required]
		[MaxLength(256)]
		public string Application { get; set; } = string.Empty;

		[Required]
		[MaxLength(32)]
		public string Version { get; set; } = string.Empty;

		public DateTime SessionStartUtc { get; set; }

		public DateTime SessionEndUtc { get; set; }

		public double SessionActiveMinutes { get; set; }

		public long LogFileSizeBytes { get; set; }

		public long InstalledRamMb { get; set; }

		public int FilterExecutionCount { get; set; }

		public int GraphOpenCount { get; set; }

		public int DashboardOpenCount { get; set; }

		[Required]
		[MaxLength(16)]
		public string SchemaVersion { get; set; } = string.Empty;
	}
}
