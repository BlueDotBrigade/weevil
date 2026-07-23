namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;

	internal static class TelemetryConsent
	{
		internal const string TelemetryEnabledEnvironmentVariable = "WEEVIL_TELEMETRY_ENABLED";

		public static bool IsEnabled()
		{
			var value = Environment.GetEnvironmentVariable(TelemetryEnabledEnvironmentVariable);
			return IsEnabled(value);
		}

		public static bool IsEnabled(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			return string.Equals(value, "1", StringComparison.Ordinal) ||
				string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
		}
	}
}
