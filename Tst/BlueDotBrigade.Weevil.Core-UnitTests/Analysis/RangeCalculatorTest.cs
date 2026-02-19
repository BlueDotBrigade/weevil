namespace BlueDotBrigade.Weevil.Statistics
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
			Assert.IsNotNull(result);
			Assert.AreEqual(new DateTime(2024, 1, 1, 10, 0, 0), result.StartAt);
			Assert.AreEqual(new DateTime(2024, 1, 1, 10, 0, 10), result.EndAt);
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
			Assert.IsNull(result.StartAt);
			Assert.IsNull(result.EndAt);
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
			Assert.AreEqual(new DateTime(2024, 6, 15, 12, 0, 0), result.StartAt);
			Assert.AreEqual(new DateTime(2024, 6, 15, 12, 0, 0), result.EndAt);
		}
	}
}
