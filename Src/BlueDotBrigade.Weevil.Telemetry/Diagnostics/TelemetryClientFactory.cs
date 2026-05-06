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
		private const string TelemetryUserNameEnvironmentVariable = "WEEVIL_TELEMETRY_USERNAME";
		private const string TelemetrySecretEnvironmentVariable = "WEEVIL_TELEMETRY_SECRET";

		/// <summary>
		/// Creates a telemetry client based on runtime credential configuration.
		/// </summary>
		public static ITelemetryClient Create()
		{
			MsSqlTelemetryClientOptions options = CreateOptions(TelemetryConfiguration.GetConnectionString());

			if (string.IsNullOrWhiteSpace(options.UsernameOrApiToken) &&
				string.IsNullOrWhiteSpace(options.Secret))
			{
				return NullTelemetryClient.Instance;
			}

			return new MsSqlTelemetryClient(options);
		}

		internal static MsSqlTelemetryClientOptions CreateOptions(string connectionString)
		{
			return new MsSqlTelemetryClientOptions
			{
				ConnectionString = connectionString,
				UsernameOrApiToken = GetOptionalEnvironmentValue(TelemetryUserNameEnvironmentVariable),
				Secret = SecretProtector.Decrypt(GetOptionalEnvironmentValue(TelemetrySecretEnvironmentVariable)),
			};
		}

		private static string GetOptionalEnvironmentValue(string variableName)
		{
			var value = Environment.GetEnvironmentVariable(variableName);
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
		}
	}
}
