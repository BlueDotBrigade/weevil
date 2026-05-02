namespace BlueDotBrigade.Weevil.Diagnostics
{
	using BlueDotBrigade.Weevil.Data.SqlClient;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TelemetryClientFactoryTests
	{
		[TestMethod]
		public void GivenTelemetryDisabled_WhenCreateCalled_ThenNullTelemetryClientIsReturned()
		{
			// Regression: Issue #761
			var client = TelemetryClientFactory.Create(false);

			client.Should().BeSameAs(NullTelemetryClient.Instance);
		}

		[TestMethod]
		public void GivenTelemetryEnabled_WhenCreateCalled_ThenMsSqlTelemetryClientIsReturned()
		{
			// Regression: Issue #761
			var client = TelemetryClientFactory.Create(true);

			client.Should().BeOfType<MsSqlTelemetryClient>();
		}
	}
}
