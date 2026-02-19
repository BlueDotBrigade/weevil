namespace BlueDotBrigade.Weevil.Statistics
{
	using System;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class StandardDeviationCalculatorTest
	{
		[TestMethod]
		public void Calculate_WithValidValues_ReturnsCorrectStdDev()
		{
			// Arrange
			var calculator = new StandardDeviationCalculator();
			var values = new List<double> { 1, 2, 3, 4, 5 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("StdDev", result.Key);
			Assert.IsNotNull(result.Value);
			// Population std dev of [1,2,3,4,5]: sqrt(2) ≈ 1.414
			Assert.AreEqual(1.414, (double)result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsNull()
		{
			// Arrange
			var calculator = new StandardDeviationCalculator();
			var values = new List<double>();
			var timestamps = new List<DateTime>();

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("StdDev", result.Key);
			Assert.IsNull(result.Value);
		}

		[TestMethod]
		public void Calculate_WithIdenticalValues_ReturnsZero()
		{
			// Arrange
			var calculator = new StandardDeviationCalculator();
			var values = new List<double> { 5, 5, 5, 5 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual(0.0, (double)result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithResult_RoundsToThreeDecimalPlaces()
		{
			// Arrange
			var calculator = new StandardDeviationCalculator();
			var values = new List<double> { 5, 10, 15 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			// Mean = 10, deviations: [-5, 0, 5], sum of squares = 50, variance = 50/3 ≈ 16.667, stddev ≈ 4.082
			Assert.AreEqual(4.082, (double)result.Value, 0.001);
		}
	}
}
