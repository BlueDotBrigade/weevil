namespace BlueDotBrigade.Weevil.Math
{
	using System;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class MeanCalculatorTest
	{
		[TestMethod]
		public void Calculate_WithValidValues_ReturnsCorrectMean()
		{
			// Arrange
			var calculator = new MeanCalculator();
			var values = new List<double> { 1, 2, 3, 4, 5 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(3.0, result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsNull()
		{
			// Arrange
			var calculator = new MeanCalculator();
			var values = new List<double>();

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.IsNull(result);
		}

		[TestMethod]
		public void Calculate_WithSingleValue_ReturnsThatValue()
		{
			// Arrange
			var calculator = new MeanCalculator();
			var values = new List<double> { 42.5 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.AreEqual(42.5, result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithResult_RoundsToThreeDecimalPlaces()
		{
			// Arrange
			var calculator = new MeanCalculator();
			var values = new List<double> { 1, 2, 3 }; // mean = 2.0

			// Act
			var result = calculator.Calculate(values);

			// Assert
			// Mean of [1, 2, 3] = 2.0 (exact)
			Assert.AreEqual(2.0, result.Value, 0.001);
		}
	}
}
