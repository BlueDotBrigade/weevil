namespace BlueDotBrigade.Weevil.Configuration
{
	using FluentAssertions;

	[TestClass]
	public class TelemetryConfigurationTests
	{
		[TestMethod]
		public void GivenTelemetryConfiguration_WhenGetConnectionStringCalled_ThenReturnsEmbeddedConnectionString()
		{
			// Regression: Issue #802
			var connectionString = TelemetryConfiguration.GetConnectionString();

			connectionString.Should().NotBeNullOrWhiteSpace();
			connectionString.Should().Contain("Server=tcp:");
			connectionString.Should().Contain(".database.windows.net");
			connectionString.Should().Contain("Initial Catalog=WeevilTelemetry");
		}
	}
}
