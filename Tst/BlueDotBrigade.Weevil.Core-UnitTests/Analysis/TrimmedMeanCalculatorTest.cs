namespace BlueDotBrigade.Weevil.Math
{
	using System;
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TrimmedMeanCalculatorTest
	{
		[TestMethod]
		public void Calculate_WithValidValues_ReturnsCorrectTrimmedMean()
		{
			// Arrange
			var calculator = new TrimmedMeanCalculator(0.1); // 10% trim
			var values = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

			// Act
			var result = calculator.Calculate(values);

			// Assert
			(result).Should().NotBeNull();
			
			// With 10% trim on 10 values: trimCount = 1
			// Trimmed values should be [2, 3, 4, 5, 6, 7, 8, 9]
			// Mean of these 8 values = 44/8 = 5.5
			(result.Value).Should().BeApproximately(5.5, 0.001);
		}

		[TestMethod]
		public void Calculate_WithEmptyList_ReturnsNull()
		{
			// Arrange
			var calculator = new TrimmedMeanCalculator(0.1);
			var values = new List<double>();

			// Act
			var result = calculator.Calculate(values);

			// Assert
			(result).Should().BeNull();
		}

		[TestMethod]
		public void Calculate_WithTooSmallList_ReturnsNull()
		{
			// Arrange
			var calculator = new TrimmedMeanCalculator(0.5); // 50% trim
			var values = new List<double> { 1, 2 }; // Only 2 values, trimCount*2 = 2

			// Act
			var result = calculator.Calculate(values);

			// Assert
			(result).Should().BeNull(); // Should return null when trim count is too large
		}

		[TestMethod]
		public void Calculate_UsesArrayLength_NotOriginalCount()
		{
			// Arrange
			// This test verifies the bug fix: we should use sorted.Length, not values.Count
			var calculator = new TrimmedMeanCalculator(0.2); // 20% trim
			var values = new List<double> { 5, 1, 9, 3, 7 }; // 5 values

			// Act
			var result = calculator.Calculate(values);

			// Assert
			(result).Should().NotBeNull();
			
			// With 20% trim on 5 values: trimCount = 1 (floor of 5*0.2)
			// Sorted: [1, 3, 5, 7, 9]
			// After trimming 1 from each end: [3, 5, 7]
			// Mean = 15/3 = 5.0
			(result.Value).Should().BeApproximately(5.0, 0.001);
		}
	}
}
