namespace BlueDotBrigade.Weevil.Configuration
{
	using System;

	public static class TelemetryConfiguration
	{
		// Non-secret telemetry endpoint configuration.
		// Credentials are supplied at runtime via WEEVIL_TELEMETRY_USERNAME and WEEVIL_TELEMETRY_SECRET.
		// Encrypt=True : Create a secure tunnel by using SSL/TLS so nobody can snoop on the data traveling between Weevil and Azure.
		// TrustServerCertificate=False : Do NOT blindly trust the server certificate. Validate it properly.
		private const string EmbeddedConnectionString =
			@"Server=tcp:weevil-db.database.windows.net,1433;Initial Catalog=weevil;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

		/// <summary>
		/// Returns the telemetry database connection string embedded in the application.
		/// </summary>
		public static string GetConnectionString()
		{
			return EmbeddedConnectionString;
		}
	}
}
