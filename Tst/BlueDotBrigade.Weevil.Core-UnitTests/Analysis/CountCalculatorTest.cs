namespace BlueDotBrigade.Weevil.Math
{
	using System;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class CountCalculatorTest
	{
		[TestMethod]
		public void Calculate_WithValidValues_ReturnsCorrectCount()
		{
			// Arrange
			var calculator = new CountCalculator();
			var values = new List<double> { 1, 2, 3, 4, 5 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(5.0, result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsZero()
		{
			// Arrange
			var calculator = new CountCalculator();
			var values = new List<double>();

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(0.0, result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithSingleValue_ReturnsOne()
		{
			// Arrange
			var calculator = new CountCalculator();
			var values = new List<double> { 42.5 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			Assert.AreEqual(1.0, result.Value, 0.001);
		}
	}
}
