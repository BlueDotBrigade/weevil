namespace BlueDotBrigade.Weevil.Configuration
{
	public static class TelemetryConfiguration
	{
		// Non-secret telemetry endpoint configuration.
		// Credentials are supplied at runtime via WEEVIL_TELEMETRY_USERNAME and WEEVIL_TELEMETRY_SECRET.
		private const string EmbeddedConnectionString =
			"Server=tcp:weevil-telemetry.database.windows.net,1433;Initial Catalog=WeevilTelemetry;";


		/// <summary>
		/// Returns the telemetry database connection string embedded in the application.
		/// </summary>
		public static string GetConnectionString()
		{
			return EmbeddedConnectionString;
		}
	}
}
