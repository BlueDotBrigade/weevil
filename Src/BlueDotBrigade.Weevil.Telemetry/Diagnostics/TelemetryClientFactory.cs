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
		private const string TelemetrySourceEnvironmentVariable = "WEEVIL_TELEMETRY_SOURCE";
		private const string DevelopmentSource = "Development";

		/// <summary>
		/// Creates a telemetry client based on runtime credential configuration.
		/// </summary>
		/// <remarks>
		/// Telemetry must never crash the host application: any failure while reading configuration or
		/// decrypting credentials (for example, a secret encrypted on a different machine) is swallowed
		/// and the no-op <see cref="NullTelemetryClient"/> is returned instead.
		/// </remarks>
		// Intentional broad exception catching: telemetry setup must never propagate to the user workflow.
#pragma warning disable CA1031
		public static ITelemetryClient Create()
		{
			try
			{
				MsSqlTelemetryClientOptions options = CreateOptions(TelemetryConfiguration.GetConnectionString());

				if (string.IsNullOrWhiteSpace(options.UsernameOrApiToken) &&
					string.IsNullOrWhiteSpace(options.Secret))
				{
					Log.Default.Write(LogSeverityType.Warning, "Telemetry credentials have not been provided - telemetry will not be saved in the centralied repository.");
					return NullTelemetryClient.Instance;
				}

				Log.Default.Write(LogSeverityType.Information, "Telemetry credentials have been provided.");

				return new MsSqlTelemetryClient(options);
			}
			catch (Exception exception)
			{
				try
				{
					Log.Default.Write(LogSeverityType.Error, exception, "Telemetry client creation failed - telemetry is disabled for this session.");
				}
				catch
				{
					// Logging must not crash the host either.
				}

				return NullTelemetryClient.Instance;
			}
		}
#pragma warning restore CA1031

		internal static MsSqlTelemetryClientOptions CreateOptions(string connectionString)
		{
			return new MsSqlTelemetryClientOptions
			{
				ConnectionString = connectionString,
				UsernameOrApiToken = GetOptionalEnvironmentValue(TelemetryUserNameEnvironmentVariable),
				Secret = SecretProtector.Decrypt(GetOptionalEnvironmentValue(TelemetrySecretEnvironmentVariable)),
			};
		}

     public static string GetTelemetrySource()
		{
			var source = GetOptionalEnvironmentValue(TelemetrySourceEnvironmentVariable);
			return string.IsNullOrWhiteSpace(source) ? DevelopmentSource : source;
		}

		private static string GetOptionalEnvironmentValue(string variableName)
		{
			var value = Environment.GetEnvironmentVariable(variableName);
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
		}
	}
}
