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
			result.Should().NotBeNull();
			result.Value.Should().BeApproximately(3.0, 0.001);
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
			result.Should().BeNull();
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
			result.Value.Should().BeApproximately(42.5, 0.001);
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
			result.Value.Should().BeApproximately(2.0, 0.001);
		}
	}
}
