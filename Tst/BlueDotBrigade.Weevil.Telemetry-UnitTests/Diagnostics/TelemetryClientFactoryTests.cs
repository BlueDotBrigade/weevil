namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Reflection;
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

		[TestMethod]
		public void GivenEnvironmentCredentials_WhenCreateCalled_ThenCredentialsAreStoredInClientOptions()
		{
			// Regression: Issue #802
			const string userNameVariable = "WEEVIL_TELEMETRY_SQL_USERNAME";
			const string passwordVariable = "WEEVIL_TELEMETRY_SQL_PASSWORD_OR_API_TOKEN";
			var originalUserName = Environment.GetEnvironmentVariable(userNameVariable);
			var originalPassword = Environment.GetEnvironmentVariable(passwordVariable);

			try
			{
				Environment.SetEnvironmentVariable(userNameVariable, "telemetry-user");
				Environment.SetEnvironmentVariable(passwordVariable, "telemetry-secret");

				var client = TelemetryClientFactory.Create(true).Should().BeOfType<MsSqlTelemetryClient>().Subject;

				var optionsField = typeof(MsSqlTelemetryClient).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);
				optionsField.Should().NotBeNull();

				var options = optionsField.GetValue(client).Should().BeOfType<MsSqlTelemetryClientOptions>().Subject;
				options.UserName.Should().Be("telemetry-user");
				options.PasswordOrApiToken.Should().Be("telemetry-secret");
			}
			finally
			{
				Environment.SetEnvironmentVariable(userNameVariable, originalUserName);
				Environment.SetEnvironmentVariable(passwordVariable, originalPassword);
			}
		}
	}
}
