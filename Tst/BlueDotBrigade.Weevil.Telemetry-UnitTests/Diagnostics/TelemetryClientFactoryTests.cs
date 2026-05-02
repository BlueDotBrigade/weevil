namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using BlueDotBrigade.Weevil.Data.SqlClient;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TelemetryClientFactoryTests
	{
		private const string UserNameVariable = "WEEVIL_TELEMETRY_USERNAME";
		private const string SecretVariable = "WEEVIL_TELEMETRY_SECRET";

		[TestMethod]
		public void GivenCredentialsMissing_WhenCreateCalled_ThenNullTelemetryClientIsReturned()
		{
			// Regression: Issue #761
			var originalUserName = Environment.GetEnvironmentVariable(UserNameVariable);
			var originalSecret = Environment.GetEnvironmentVariable(SecretVariable);

			try
			{
				Environment.SetEnvironmentVariable(UserNameVariable, null);
				Environment.SetEnvironmentVariable(SecretVariable, null);

				var client = TelemetryClientFactory.Create();
				client.Should().BeSameAs(NullTelemetryClient.Instance);
			}
			finally
			{
				Environment.SetEnvironmentVariable(UserNameVariable, originalUserName);
				Environment.SetEnvironmentVariable(SecretVariable, originalSecret);
			}
		}

		[TestMethod]
		public void GivenCredentialsProvided_WhenCreateCalled_ThenMsSqlTelemetryClientIsReturned()
		{
			// Regression: Issue #761
			var originalUserName = Environment.GetEnvironmentVariable(UserNameVariable);
			var originalSecret = Environment.GetEnvironmentVariable(SecretVariable);

			try
			{
				Environment.SetEnvironmentVariable(UserNameVariable, "telemetry-user");
				Environment.SetEnvironmentVariable(SecretVariable, "telemetry-secret");

				var client = TelemetryClientFactory.Create();

				client.Should().BeOfType<MsSqlTelemetryClient>();
			}
			finally
			{
				Environment.SetEnvironmentVariable(UserNameVariable, originalUserName);
				Environment.SetEnvironmentVariable(SecretVariable, originalSecret);
			}
		}

		[TestMethod]
		public void GivenEnvironmentCredentials_WhenCreateCalled_ThenCredentialsAreStoredInClientOptions()
		{
			// Regression: Issue #802
			var originalUserName = Environment.GetEnvironmentVariable(UserNameVariable);
			var originalSecret = Environment.GetEnvironmentVariable(SecretVariable);

			try
			{
				Environment.SetEnvironmentVariable(UserNameVariable, "telemetry-user");
				Environment.SetEnvironmentVariable(SecretVariable, "telemetry-secret");

				var options = TelemetryClientFactory.CreateOptions("Server=localhost;Database=Weevil;");
				options.UsernameOrApiToken.Should().Be("telemetry-user");
				options.Secret.Should().Be("telemetry-secret");
			}
			finally
			{
				Environment.SetEnvironmentVariable(UserNameVariable, originalUserName);
				Environment.SetEnvironmentVariable(SecretVariable, originalSecret);
			}
		}

		[TestMethod]
		public void GivenCredentialsBlank_WhenCreateCalled_ThenNullTelemetryClientIsReturned()
		{
			// Regression: Issue #802
			var originalUserName = Environment.GetEnvironmentVariable(UserNameVariable);
			var originalSecret = Environment.GetEnvironmentVariable(SecretVariable);

			try
			{
				Environment.SetEnvironmentVariable(UserNameVariable, string.Empty);
				Environment.SetEnvironmentVariable(SecretVariable, string.Empty);

				var client = TelemetryClientFactory.Create();
				client.Should().BeSameAs(NullTelemetryClient.Instance);
			}
			finally
			{
				Environment.SetEnvironmentVariable(UserNameVariable, originalUserName);
				Environment.SetEnvironmentVariable(SecretVariable, originalSecret);
			}
		}
	}
}
