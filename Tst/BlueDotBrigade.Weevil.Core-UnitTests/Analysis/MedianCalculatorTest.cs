namespace BlueDotBrigade.Weevil.Math
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

			// Act
			var result = calculator.Calculate(values);

			// Assert
			result.Should().NotBeNull();
			result.Value.Should().BeApproximately(3.0, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEvenNumberOfValues_ReturnsAverageOfMiddleTwo()
		{
			// Arrange
			var calculator = new MedianCalculator();
			var values = new List<double> { 1, 2, 3, 4 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			// Median of [1, 2, 3, 4] = (2 + 3) / 2 = 2.5
			result.Value.Should().BeApproximately(2.5, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsNull()
		{
			// Arrange
			var calculator = new MedianCalculator();
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
			var calculator = new MedianCalculator();
			var values = new List<double> { 7.0 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			result.Value.Should().BeApproximately(7.0, 0.001);
		}
	}
}
