namespace BlueDotBrigade.Weevil.Math
{
	using System;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class RangeCalculatorTest
	{
		[TestMethod]
		public void Calculate_WithValidTimestamps_ReturnsCorrectRange()
		{
			// Arrange
			var calculator = new RangeCalculator();
			var timestamps = new List<DateTime>
			{
				new DateTime(2024, 1, 1, 10, 0, 0),
				new DateTime(2024, 1, 1, 10, 0, 5),
				new DateTime(2024, 1, 1, 10, 0, 10),
			};

			// Act
			var result = calculator.Calculate(timestamps);

			// Assert
			result.Should().NotBeNull();
			result.StartAt.Should().Be(new DateTime(2024, 1, 1, 10, 0, 0));
			result.EndAt.Should().Be(new DateTime(2024, 1, 1, 10, 0, 10));
		}

		[TestMethod]
		public void Calculate_WithEmptyTimestamps_ReturnsNullRange()
		{
			// Arrange
			var calculator = new RangeCalculator();
			var timestamps = new List<DateTime>();

			// Act
			var result = calculator.Calculate(timestamps);

			// Assert
			result.StartAt.Should().BeNull();
			result.EndAt.Should().BeNull();
		}

		[TestMethod]
		public void Calculate_WithSingleTimestamp_ReturnsMatchingStartAndEnd()
		{
			// Arrange
			var calculator = new RangeCalculator();
			var timestamps = new List<DateTime> { new DateTime(2024, 6, 15, 12, 0, 0) };

			// Act
			var result = calculator.Calculate(timestamps);

			// Assert
			result.StartAt.Should().Be(new DateTime(2024, 6, 15, 12, 0, 0));
			result.EndAt.Should().Be(new DateTime(2024, 6, 15, 12, 0, 0));
		}
	}
}
