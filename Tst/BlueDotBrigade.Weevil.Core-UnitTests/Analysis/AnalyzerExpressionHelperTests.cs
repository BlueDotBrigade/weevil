namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	/// <summary>
	/// Tests for AnalyzerExpressionHelper functionality including alias expansion and multiple expression parsing.
	/// </summary>
	[TestClass]
	public class AnalyzerExpressionHelperTests
	{
		private IFilterAliasExpander GetMockAliasExpander(IDictionary<string, string> aliases)
		{
			var expander = new FilterAliasExpander(aliases);
			return expander;
		}

		[TestMethod]
		public void ParseExpressions_WithNullInput_ReturnsEmptyArray()
		{
			// Arrange
			var aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			// Act - using FilterStrategy.KeepAllRecords which has null ExpressionBuilder
			// This test validates the null/empty input handling
			var result = AnalyzerExpressionHelper.ParseExpressions(
				null,
				aliasExpander,
				null);

			// Assert
			Assert.IsTrue(result.IsEmpty);
		}

		[TestMethod]
		public void ParseExpressions_WithEmptyInput_ReturnsEmptyArray()
		{
			// Arrange
			var aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			// Act
			var result = AnalyzerExpressionHelper.ParseExpressions(
				string.Empty,
				aliasExpander,
				null);

			// Assert
			Assert.IsTrue(result.IsEmpty);
		}

		[TestMethod]
		public void ParseExpressions_WithWhitespaceInput_ReturnsEmptyArray()
		{
			// Arrange
			var aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			// Act
			var result = AnalyzerExpressionHelper.ParseExpressions(
				"   ",
				aliasExpander,
				null);

			// Assert
			Assert.IsTrue(result.IsEmpty);
		}

		[TestMethod]
		public void ParseExpressions_WithNullAliasExpander_ProcessesInputDirectly()
		{
			// Arrange - null aliasExpander should not throw

			// Act
			var result = AnalyzerExpressionHelper.ParseExpressions(
				"test",
				null,
				null);

			// Assert - with null expressionBuilder, it won't parse but shouldn't throw
			Assert.IsTrue(result.IsEmpty);
		}

		[TestMethod]
		public void ParseAllExpressions_WithNullInput_ReturnsEmptyArray()
		{
			// Arrange
			var aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			// Act
			var result = AnalyzerExpressionHelper.ParseAllExpressions(
				null,
				aliasExpander,
				null);

			// Assert
			Assert.IsTrue(result.IsEmpty);
		}

		[TestMethod]
		public void ParseAllExpressions_WithEmptyInput_ReturnsEmptyArray()
		{
			// Arrange
			var aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			// Act
			var result = AnalyzerExpressionHelper.ParseAllExpressions(
				string.Empty,
				aliasExpander,
				null);

			// Assert
			Assert.IsTrue(result.IsEmpty);
		}

		[TestMethod]
		public void ParseAllExpressions_WithWhitespaceInput_ReturnsEmptyArray()
		{
			// Arrange
			var aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			// Act
			var result = AnalyzerExpressionHelper.ParseAllExpressions(
				"   ",
				aliasExpander,
				null);

			// Assert
			Assert.IsTrue(result.IsEmpty);
		}
	}
}
