namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Linq;

	internal static class TelemetrySessionMapper
	{
		public static TelemetrySessionDto ToDto(TelemetrySession session)
		{
			if (session == null)
			{
				throw new ArgumentNullException(nameof(session));
			}

			var dto = new TelemetrySessionDto
			{
				SessionId = session.SessionId,
				Application = session.Application ?? string.Empty,
				Source = session.Source ?? string.Empty,
				Version = session.Version?.ToString() ?? "0.0",
				IsDebugging = session.IsDebugging,
				SessionStartUtc = session.SessionStartUtc,
				SessionEndUtc = session.SessionEndUtc,
				SessionActiveMinutes = session.SessionActiveMinutes,
				LogFileSizeBytes = session.LogFileSizeBytes,
				InstalledRamMb = session.InstalledRamMb,
				InstalledCpu = session.InstalledCpu ?? string.Empty,
				SchemaVersion = string.IsNullOrWhiteSpace(session.SchemaVersion) ? "1.0" : session.SchemaVersion,
				Metrics = session.Metrics
					.Select(metric => new TelemetrySessionMetricDto
					{
						MetricKey = metric.MetricKey,
						MetricCount = metric.MetricCount,
					})
					.ToList(),
			};

			return dto;
		}

		public static TelemetrySession ToSession(TelemetrySessionDto session)
		{
			if (session == null)
			{
				throw new ArgumentNullException(nameof(session));
			}

			var result = new TelemetrySession
			{
				SessionId = session.SessionId,
				Application = session.Application ?? string.Empty,
				Source = session.Source ?? string.Empty,
				Version = Version.TryParse(session.Version, out var version) ? version : new Version(0, 0),
				IsDebugging = session.IsDebugging,
				SessionStartUtc = session.SessionStartUtc,
				SessionEndUtc = session.SessionEndUtc,
				SessionActiveMinutes = session.SessionActiveMinutes,
				LogFileSizeBytes = session.LogFileSizeBytes,
				InstalledRamMb = session.InstalledRamMb,
				InstalledCpu = session.InstalledCpu ?? string.Empty,
				SchemaVersion = string.IsNullOrWhiteSpace(session.SchemaVersion) ? "1.0" : session.SchemaVersion,
			};

			foreach (TelemetrySessionMetricDto metric in session.Metrics ?? Enumerable.Empty<TelemetrySessionMetricDto>())
			{
				result.Metrics.Add(new TelemetrySessionMetric
				{
					SessionId = result.SessionId,
					MetricKey = metric.MetricKey,
					MetricCount = metric.MetricCount,
					Session = result,
				});
			}

			return result;
		}
	}
}