namespace BlueDotBrigade.Weevil.Math
{
	using System;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class MinCalculatorTest
	{
		[TestMethod]
		public void Calculate_WithValidValues_ReturnsSmallestValue()
		{
			// Arrange
			var calculator = new MinCalculator();
			var values = new List<double> { 5, 1, 9, 3, 7 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(1.0, result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsNull()
		{
			// Arrange
			var calculator = new MinCalculator();
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
			var calculator = new MinCalculator();
			var values = new List<double> { 42.5 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.AreEqual(42.5, result.Value, 0.001);
		}
	}
}
