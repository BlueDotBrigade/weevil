namespace BlueDotBrigade.Weevil.Common
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TimeSpanExtensionTests
	{
		[TestMethod]
		public void ToHumanReadable_TimeSpanMinimum_ReturnsDefault()
		{
			var period = TimeSpan.MinValue;

			(period.ToHumanReadable()).Should().Be("--.---");
		}

		[TestMethod]
		public void ToHumanReadable_TimeSpanMaximum_ReturnsDefault()
		{
			var period = TimeSpan.MaxValue;

			(period.ToHumanReadable()).Should().Be("--.---");
		}

		[TestMethod]
		public void ToHumanReadable_LessThan1Minute_ReturnsSecondsFormat()
		{
			var period = TimeSpan.FromMilliseconds(1);

			(period.ToHumanReadable()).Should().Be("00.001s");
		}

		[TestMethod]
		public void ToHumanReadable_MoreThan1Minute_ReturnsHourFormat()
		{
			var period = TimeSpan.FromMinutes(2);

			(period.ToHumanReadable()).Should().Be("00:02:00");
		}

		[TestMethod]
		public void ToHumanReadable_MoreThan1Day_ReturnsDayFormat()
		{
			var period = TimeSpan.FromDays(3);

			(period.ToHumanReadable()).Should().Be("3.00:00:00");
		}

		[TestMethod]
		public void ToHumanReadable_LessThan1MinuteNegative_ReturnsSecondsFormat()
		{
			var period = TimeSpan.FromMilliseconds(-1);

			(period.ToHumanReadable()).Should().Be("-00.001s");
		}

		[TestMethod]
		public void ToHumanReadable_MoreThan1MinuteNegative_ReturnsHourFormat()
		{
			var period = TimeSpan.FromMinutes(-2);

			(period.ToHumanReadable()).Should().Be("-00:02:00");
		}

		[TestMethod]
		public void ToHumanReadable_MoreThan1DayNegative_ReturnsDayFormat()
		{
			var period = TimeSpan.FromDays(-3);

			(period.ToHumanReadable()).Should().Be("-3.00:00:00");
		}
	}
}
