namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TelemetryActiveUsageAccumulatorTests
	{
		[TestMethod]
		public void GivenElapsedDurationLongerThanLease_WhenRenewed_ThenAccumulationIsCapped()
		{
			var start = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);
			var accumulator = new TelemetryActiveUsageAccumulator(TimeSpan.FromMinutes(15));
			accumulator.Reset(start);

			accumulator.Renew(start.AddMinutes(45));

			accumulator.ActiveMinutes.Should().Be(15);
		}

		[TestMethod]
		public void GivenNonPositiveElapsedDuration_WhenRenewed_ThenActiveMinutesDoNotIncrease()
		{
			var start = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);
			var accumulator = new TelemetryActiveUsageAccumulator(TimeSpan.FromMinutes(15));
			accumulator.Reset(start);
			accumulator.Renew(start);
			accumulator.Renew(start.AddMinutes(-5));

			accumulator.ActiveMinutes.Should().Be(0);
		}
	}
}
