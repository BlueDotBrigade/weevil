namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class NullTelemetryClientTests
	{
		[TestMethod]
		public async Task GivenTelemetrySession_WhenSendAsyncCalled_ThenCompletesWithoutThrowing()
		{
			// Arrange
			var client = new NullTelemetryClient();
			var session = new TelemetrySession();

			// Act
			Func<Task> act = async () => await client.SendAsync(session, CancellationToken.None);

			// Assert
			await act.Should().NotThrowAsync();
		}

		[TestMethod]
		public async Task GivenNullTelemetrySession_WhenSendAsyncCalled_ThenCompletesWithoutThrowing()
		{
			// Arrange
			var client = new NullTelemetryClient();

			// Act
			Func<Task> act = async () => await client.SendAsync(null, new CancellationToken(canceled: true));

			// Assert
			await act.Should().NotThrowAsync();
		}

		[TestMethod]
		public void GivenTelemetrySession_WhenSendSyncCalled_ThenDoesNotThrow()
		{
			// Arrange
			var client = new NullTelemetryClient();
			var session = new TelemetrySession();

			// Act
			Action act = () => client.SendSync(session);

			// Assert
			act.Should().NotThrow();
		}

		[TestMethod]
		public void GivenNullTelemetrySession_WhenSendSyncCalled_ThenDoesNotThrow()
		{
			// Arrange
			var client = new NullTelemetryClient();

			// Act
			Action act = () => client.SendSync(null);

			// Assert
			act.Should().NotThrow();
		}
	}
}
