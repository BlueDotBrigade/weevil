namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using BlueDotBrigade.Weevil.Configuration;
	using BlueDotBrigade.Weevil.Data.SqlClient;

	/// <summary>
	/// Creates the telemetry client used by application entry points.
	/// </summary>
	public static class TelemetryClientFactory
	{
		private const string TelemetrySqlUserNameEnvironmentVariable = "WEEVIL_TELEMETRY_SQL_USERNAME";
		private const string TelemetrySqlPasswordOrApiTokenEnvironmentVariable = "WEEVIL_TELEMETRY_SQL_PASSWORD_OR_API_TOKEN";

		/// <summary>
		/// Creates a telemetry client based on the runtime enabled flag.
		/// </summary>
		public static ITelemetryClient Create(bool isTelemetryEnabled)
		{
			if (!isTelemetryEnabled)
			{
				return NullTelemetryClient.Instance;
			}

			return new MsSqlTelemetryClient(new MsSqlTelemetryClientOptions
			{
				ConnectionString = TelemetryConfiguration.GetConnectionString(),
				UserName = GetOptionalEnvironmentValue(TelemetrySqlUserNameEnvironmentVariable),
				PasswordOrApiToken = GetOptionalEnvironmentValue(TelemetrySqlPasswordOrApiTokenEnvironmentVariable),
			});
		}

		private static string GetOptionalEnvironmentValue(string variableName)
		{
			var value = Environment.GetEnvironmentVariable(variableName);
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
		}
	}
}
