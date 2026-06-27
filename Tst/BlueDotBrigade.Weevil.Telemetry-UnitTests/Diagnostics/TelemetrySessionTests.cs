namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Linq;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TelemetrySessionTests
	{
		[TestMethod]
		public void GivenUnseenMetricKey_WhenIncrement_ThenMetricAddedWithCountOne()
		{
			// Arrange
			var sessionId = Guid.NewGuid();
			var session = new TelemetrySession { SessionId = sessionId };

			// Act
			session.Increment("Filter.Applied");

			// Assert
			session.Metrics.Should().ContainSingle();
			TelemetrySessionMetric metric = session.Metrics.Single();
			metric.MetricKey.Should().Be("Filter.Applied");
			metric.MetricCount.Should().Be(1);
			metric.SessionId.Should().Be(sessionId);
		}

		[TestMethod]
		public void GivenExistingMetricKey_WhenIncrementedAgain_ThenSameMetricCountIncreases()
		{
			// Arrange
			var session = new TelemetrySession { SessionId = Guid.NewGuid() };
			session.Increment("Filter.Applied");

			// Act
			session.Increment("Filter.Applied");
			session.Increment("Filter.Applied");

			// Assert
			session.Metrics.Should().ContainSingle();
			session.Metrics.Single().MetricCount.Should().Be(3);
		}

		[TestMethod]
		public void GivenDifferentMetricKeys_WhenIncremented_ThenEachKeyIsTrackedSeparately()
		{
			// Arrange
			var session = new TelemetrySession { SessionId = Guid.NewGuid() };

			// Act
			session.Increment("Filter.Applied");
			session.Increment("Navigation.GoToLine");
			session.Increment("Filter.Applied");

			// Assert
			session.Metrics.Should().HaveCount(2);
			session.Metrics.Single(m => m.MetricKey == "Filter.Applied").MetricCount.Should().Be(2);
			session.Metrics.Single(m => m.MetricKey == "Navigation.GoToLine").MetricCount.Should().Be(1);
		}

		[TestMethod]
		public void GivenWhitespaceMetricKey_WhenIncrement_ThenThrows()
		{
			// Arrange
			var session = new TelemetrySession();

			// Act
			Action act = () => session.Increment("   ");

			// Assert
			act.Should().Throw<ArgumentException>();
		}
	}
}
