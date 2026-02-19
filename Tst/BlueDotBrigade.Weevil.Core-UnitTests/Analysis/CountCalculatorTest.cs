namespace BlueDotBrigade.Weevil.Statistics
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
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Count", result.Key);
			Assert.AreEqual(5, (int)result.Value);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsZero()
		{
			// Arrange
			var calculator = new CountCalculator();
			var values = new List<double>();
			var timestamps = new List<DateTime>();

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Count", result.Key);
			Assert.AreEqual(0, (int)result.Value);
		}

		[TestMethod]
		public void Calculate_WithSingleValue_ReturnsOne()
		{
			// Arrange
			var calculator = new CountCalculator();
			var values = new List<double> { 42.5 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual(1, (int)result.Value);
		}
	}
}
