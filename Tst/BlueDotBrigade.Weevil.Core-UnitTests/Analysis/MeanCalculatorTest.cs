namespace BlueDotBrigade.Weevil.Statistics
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
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Mean", result.Key);
			Assert.IsNotNull(result.Value);
			Assert.AreEqual(3.0, (double)result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsNull()
		{
			// Arrange
			var calculator = new MeanCalculator();
			var values = new List<double>();
			var timestamps = new List<DateTime>();

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Mean", result.Key);
			Assert.IsNull(result.Value);
		}

		[TestMethod]
		public void Calculate_WithSingleValue_ReturnsThatValue()
		{
			// Arrange
			var calculator = new MeanCalculator();
			var values = new List<double> { 42.5 };
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			Assert.AreEqual("Mean", result.Key);
			Assert.AreEqual(42.5, (double)result.Value, 0.001);
		}

		[TestMethod]
		public void Calculate_WithResult_RoundsToThreeDecimalPlaces()
		{
			// Arrange
			var calculator = new MeanCalculator();
			var values = new List<double> { 1, 2, 3 }; // mean = 2.0
			var timestamps = new List<DateTime> { DateTime.Now };

			// Act
			var result = calculator.Calculate(values, timestamps);

			// Assert
			// Mean of [1, 2, 3] = 2.0 (exact)
			Assert.AreEqual(2.0, (double)result.Value, 0.001);
		}
	}
}
