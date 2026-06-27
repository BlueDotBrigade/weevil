namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

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
		/// Telemetry schema version for the payload.
		/// </summary>
		public string SchemaVersion { get; set; } = "1.0";

		/// <summary>
		/// Extensible per-session usage counters, keyed by metric name.
		/// </summary>
		/// <remarks>
		/// Adding a new metric does not require a database schema change: each key/count pair
		/// is stored as a row in <c>dbo.telemetry_session_metric</c>.
		/// </remarks>
		public IList<TelemetrySessionMetric> Metrics { get; } = new List<TelemetrySessionMetric>();

		/// <summary>
		/// Increments the count for the supplied metric, creating it on first use.
		/// </summary>
		/// <param name="metricKey">The metric name (for example, <c>Filter.Applied</c>).</param>
		public void Increment(string metricKey)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(metricKey);

			TelemetrySessionMetric metric = Metrics.SingleOrDefault(x => x.MetricKey == metricKey);

			if (metric is null)
			{
				Metrics.Add(new TelemetrySessionMetric
				{
					SessionId = SessionId,
					MetricKey = metricKey,
					MetricCount = 1,
				});
			}
			else
			{
				metric.MetricCount++;
			}
		}
	}
}