namespace BlueDotBrigade.Weevil.Diagnostics
{
	using BlueDotBrigade.Weevil.Configuration;
	using BlueDotBrigade.Weevil.Data.SqlClient;

	/// <summary>
	/// Creates the telemetry client used by application entry points.
	/// </summary>
	public static class TelemetryClientFactory
	{
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
			});
		}
	}
}
