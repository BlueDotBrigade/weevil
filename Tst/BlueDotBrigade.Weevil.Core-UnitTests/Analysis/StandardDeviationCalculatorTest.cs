namespace BlueDotBrigade.Weevil.Math
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

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.IsNotNull(result);
			// Population std dev of [1,2,3,4,5]: sqrt(2) ≈ 1.414
			Assert.AreEqual(1.414, result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsNull()
		{
			// Arrange
			var calculator = new StandardDeviationCalculator();
			var values = new List<double>();

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.IsNull(result);
		}

		[TestMethod]
		public void Calculate_WithIdenticalValues_ReturnsZero()
		{
			// Arrange
			var calculator = new StandardDeviationCalculator();
			var values = new List<double> { 5, 5, 5, 5 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.AreEqual(0.0, result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithResult_RoundsToThreeDecimalPlaces()
		{
			// Arrange
			var calculator = new StandardDeviationCalculator();
			var values = new List<double> { 5, 10, 15 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			// Mean = 10, deviations: [-5, 0, 5], sum of squares = 50, variance = 50/3 ≈ 16.667, stddev ≈ 4.082
			Assert.AreEqual(4.082, result.Value, 0.001);
		}
	}
}
