namespace BlueDotBrigade.Weevil.Statistics
{
	using System;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class MaxCalculatorTest
	{
		[TestMethod]
		public void Calculate_WithValidValues_ReturnsLargestValue()
		{
			// Arrange
			var calculator = new MaxCalculator();
			var values = new List<double> { 5, 1, 9, 3, 7 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Max", result.Key);
			Assert.IsNotNull(result.Value);
			Assert.AreEqual(9.0, (double)result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsNull()
		{
			// Arrange
			var calculator = new MaxCalculator();
			var values = new List<double>();
			var timestamps = new List<DateTime>();

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Max", result.Key);
			Assert.IsNull(result.Value);
		}

		[TestMethod]
		public void Calculate_WithSingleValue_ReturnsThatValue()
		{
			// Arrange
			var calculator = new MaxCalculator();
			var values = new List<double> { 42.5 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual(42.5, (double)result.Value, 0.001);
		}
	}
}
