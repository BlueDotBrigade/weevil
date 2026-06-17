namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Linq;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TelemetrySessionMapperTests
	{
		[TestMethod]
		public void GivenSessionWithMetrics_WhenToDto_ThenMetricKeyAndCountAreCopied()
		{
			// Arrange
			var session = new TelemetrySession { SessionId = Guid.NewGuid() };
			session.Increment("Filter.Applied");
			session.Increment("Filter.Applied");
			session.Increment("Help.Opened");

			// Act
			TelemetrySessionDto dto = TelemetrySessionMapper.ToDto(session);

			// Assert
			dto.Metrics.Should().HaveCount(2);
			dto.Metrics.Single(m => m.MetricKey == "Filter.Applied").MetricCount.Should().Be(2);
			dto.Metrics.Single(m => m.MetricKey == "Help.Opened").MetricCount.Should().Be(1);
		}

		[TestMethod]
		public void GivenDtoWithMetrics_WhenToSession_ThenMetricsAreCopiedWithSessionId()
		{
			// Arrange
			var sessionId = Guid.NewGuid();
			var dto = new TelemetrySessionDto
			{
				SessionId = sessionId,
				Metrics =
				{
					new TelemetrySessionMetricDto { MetricKey = "Analysis.Run.DetectData", MetricCount = 4 },
				},
			};

			// Act
			TelemetrySession session = TelemetrySessionMapper.ToSession(dto);

			// Assert
			session.Metrics.Should().ContainSingle();
			TelemetrySessionMetric metric = session.Metrics.Single();
			metric.MetricKey.Should().Be("Analysis.Run.DetectData");
			metric.MetricCount.Should().Be(4);
			metric.SessionId.Should().Be(sessionId);
		}

		[TestMethod]
		public void GivenSessionWithMetrics_WhenRoundTrippedThroughDto_ThenMetricsArePreserved()
		{
			// Arrange
			var original = new TelemetrySession { SessionId = Guid.NewGuid() };
			original.Increment("Filter.Applied");
			original.Increment("Navigation.GoToLine");
			original.Increment("Navigation.GoToLine");

			// Act
			TelemetrySession roundTripped = TelemetrySessionMapper.ToSession(TelemetrySessionMapper.ToDto(original));

			// Assert
			roundTripped.Metrics
				.Select(m => (m.MetricKey, m.MetricCount))
				.Should()
				.BeEquivalentTo(original.Metrics.Select(m => (m.MetricKey, m.MetricCount)));
		}

		[TestMethod]
		public void GivenSessionWithoutSchemaVersion_WhenToDto_ThenDefaultsToCurrentSchema()
		{
			// Arrange
			var session = new TelemetrySession { SessionId = Guid.NewGuid(), SchemaVersion = string.Empty };

			// Act
			TelemetrySessionDto dto = TelemetrySessionMapper.ToDto(session);

			// Assert
			dto.SchemaVersion.Should().Be("1.0");
		}
	}
}
