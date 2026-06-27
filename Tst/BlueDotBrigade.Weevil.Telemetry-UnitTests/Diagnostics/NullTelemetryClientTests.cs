namespace BlueDotBrigade.Weevil.Diagnostics
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
			Func<Task> act = async () => await client.UploadAsync(session, CancellationToken.None);

			// Assert
			await act.Should().NotThrowAsync();
		}

		[TestMethod]
		public async Task GivenNullTelemetrySession_WhenSendAsyncCalled_ThenCompletesWithoutThrowing()
		{
			// Arrange
			var client = new NullTelemetryClient();

			// Act
			Func<Task> act = async () => await client.UploadAsync(null, new CancellationToken(canceled: true));

			// Assert
			await act.Should().NotThrowAsync();
		}
	}
}
