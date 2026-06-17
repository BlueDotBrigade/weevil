namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections.Generic;

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

		public string SchemaVersion { get; set; } = "1.0";

		public List<TelemetrySessionMetricDto> Metrics { get; set; } = new List<TelemetrySessionMetricDto>();
	}

	/// <summary>
	/// XML-friendly usage counter belonging to a <see cref="TelemetrySessionDto"/>.
	/// </summary>
	public sealed class TelemetrySessionMetricDto
	{
		public string MetricKey { get; set; } = string.Empty;

		public int MetricCount { get; set; }
	}
}