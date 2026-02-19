namespace BlueDotBrigade.Weevil.Statistics
{
	using System;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class MedianCalculatorTest
	{
		[TestMethod]
		public void Calculate_WithOddNumberOfValues_ReturnsMiddleValue()
		{
			// Arrange
			var calculator = new MedianCalculator();
			var values = new List<double> { 3, 1, 5, 2, 4 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Median", result.Key);
			Assert.IsNotNull(result.Value);
			Assert.AreEqual(3.0, (double)result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEvenNumberOfValues_ReturnsAverageOfMiddleTwo()
		{
			// Arrange
			var calculator = new MedianCalculator();
			var values = new List<double> { 1, 2, 3, 4 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			// Median of [1, 2, 3, 4] = (2 + 3) / 2 = 2.5
			Assert.AreEqual("Median", result.Key);
			Assert.AreEqual(2.5, (double)result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsNull()
		{
			// Arrange
			var calculator = new MedianCalculator();
			var values = new List<double>();
			var timestamps = new List<DateTime>();

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Median", result.Key);
			Assert.IsNull(result.Value);
		}

		[TestMethod]
		public void Calculate_WithSingleValue_ReturnsThatValue()
		{
			// Arrange
			var calculator = new MedianCalculator();
			var values = new List<double> { 7.0 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual(7.0, (double)result.Value, 0.001);
		}
	}
}
