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
			var values = new List<double> { 1, 2, 3 };
			var timestamps = new List<DateTime>
			{
				new DateTime(2024, 1, 1, 10, 0, 0),
				new DateTime(2024, 1, 1, 10, 0, 5),
				new DateTime(2024, 1, 1, 10, 0, 10),
			};

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Range", result.Key);
			Assert.IsNotNull(result.Value);
			Assert.AreEqual(new DateTime(2024, 1, 1, 10, 0, 0), calculator.Range.StartAt);
			Assert.AreEqual(new DateTime(2024, 1, 1, 10, 0, 10), calculator.Range.EndAt);
		}

		[TestMethod]
		public void Calculate_WithEmptyTimestamps_ReturnsNullRange()
		{
			// Arrange
			var calculator = new RangeCalculator();
			var values = new List<double>();
			var timestamps = new List<DateTime>();

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Range", result.Key);
			Assert.IsNull(calculator.Range.StartAt);
			Assert.IsNull(calculator.Range.EndAt);
		}

		[TestMethod]
		public void Calculate_WithSingleTimestamp_ReturnsMatchingStartAndEnd()
		{
			// Arrange
			var calculator = new RangeCalculator();
			var values = new List<double> { 42 };
			var timestamps = new List<DateTime> { new DateTime(2024, 6, 15, 12, 0, 0) };

			// Act
			calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual(new DateTime(2024, 6, 15, 12, 0, 0), calculator.Range.StartAt);
			Assert.AreEqual(new DateTime(2024, 6, 15, 12, 0, 0), calculator.Range.EndAt);
		}
	}
}
