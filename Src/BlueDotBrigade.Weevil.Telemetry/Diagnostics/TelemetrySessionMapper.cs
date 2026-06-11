namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;

	internal static class TelemetrySessionMapper
	{
		public static TelemetrySessionDto ToDto(TelemetrySession session)
		{
			if (session == null)
			{
				throw new ArgumentNullException(nameof(session));
			}

			return new TelemetrySessionDto
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
				FilterExecutionCount = session.FilterExecutionCount,
				GraphOpenCount = session.GraphOpenCount,
				DashboardOpenCount = session.DashboardOpenCount,
				HelpOpenCount = session.HelpOpenCount,
				SchemaVersion = string.IsNullOrWhiteSpace(session.SchemaVersion) ? "2.0" : session.SchemaVersion,
			};
		}

		public static TelemetrySession ToSession(TelemetrySessionDto session)
		{
			if (session == null)
			{
				throw new ArgumentNullException(nameof(session));
			}

			return new TelemetrySession
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
				FilterExecutionCount = session.FilterExecutionCount,
				GraphOpenCount = session.GraphOpenCount,
				DashboardOpenCount = session.DashboardOpenCount,
				HelpOpenCount = session.HelpOpenCount,
				SchemaVersion = string.IsNullOrWhiteSpace(session.SchemaVersion) ? "2.0" : session.SchemaVersion,
			};
		}
	}
}