namespace BlueDotBrigade.Weevil.Filter
{
	using System.Collections.Generic;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	/// <summary>
	/// Tests for FilterAliasExpander functionality.
	/// </summary>
	[TestClass]
	public class FilterAliasExpanderTests
	{
		[TestMethod]
		public void Expand_WithNoAliases_ReturnsOriginalExpression()
		{
			// Arrange
			var aliases = new Dictionary<string, string>();
			var expander = new FilterAliasExpander(aliases);

			// Act
			var result = expander.Expand("test expression");

			// Assert
			Assert.AreEqual("test expression", result);
		}

		[TestMethod]
		public void Expand_WithMatchingAlias_ReturnsExpandedExpression()
		{
			// Arrange
			var aliases = new Dictionary<string, string>
			{
				{ "#Critical", @"(?<State>Critical|Error)" }
			};
			var expander = new FilterAliasExpander(aliases);

			// Act
			var result = expander.Expand("#Critical");

			// Assert
			Assert.AreEqual(@"(?<State>Critical|Error)", result);
		}

		[TestMethod]
		public void Expand_WithNonMatchingAlias_ReturnsOriginalExpression()
		{
			// Arrange
			var aliases = new Dictionary<string, string>
			{
				{ "#Critical", @"(?<State>Critical|Error)" }
			};
			var expander = new FilterAliasExpander(aliases);

			// Act
			var result = expander.Expand("#Warning");

			// Assert
			Assert.AreEqual("#Warning", result);
		}

		[TestMethod]
		public void Expand_WithMultipleExpressionsSeparatedByPipe_ExpandsAllAliases()
		{
			// Arrange
			var aliases = new Dictionary<string, string>
			{
				{ "#Critical", @"(?<State>Critical)" },
				{ "#Error", @"(?<State>Error)" }
			};
			var expander = new FilterAliasExpander(aliases);

			// Act
			var result = expander.Expand("#Critical||#Error");

			// Assert
			Assert.AreEqual(@"(?<State>Critical)||(?<State>Error)", result);
		}

		[TestMethod]
		public void Expand_WithMixedAliasesAndRawExpressions_ExpandsOnlyAliases()
		{
			// Arrange
			var aliases = new Dictionary<string, string>
			{
				{ "#Critical", @"(?<State>Critical)" }
			};
			var expander = new FilterAliasExpander(aliases);

			// Act
			var result = expander.Expand("#Critical||(?<State>Warning)");

			// Assert
			Assert.AreEqual(@"(?<State>Critical)||(?<State>Warning)", result);
		}

		[TestMethod]
		public void Expand_WithEmptyInput_ReturnsEmptyString()
		{
			// Arrange
			var aliases = new Dictionary<string, string>
			{
				{ "#Critical", @"(?<State>Critical)" }
			};
			var expander = new FilterAliasExpander(aliases);

			// Act
			var result = expander.Expand(string.Empty);

			// Assert
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		public void Expand_WithWhitespaceInput_ReturnsEmptyString()
		{
			// Arrange
			var aliases = new Dictionary<string, string>
			{
				{ "#Critical", @"(?<State>Critical)" }
			};
			var expander = new FilterAliasExpander(aliases);

			// Act
			var result = expander.Expand("   ");

			// Assert
			Assert.AreEqual(string.Empty, result);
		}

		[TestMethod]
		public void ExpandArray_WithNoAliases_ReturnsOriginalExpressions()
		{
			// Arrange
			var aliases = new Dictionary<string, string>();
			var expander = new FilterAliasExpander(aliases);
			var expressions = new[] { "test1", "test2" };

			// Act
			var result = expander.Expand(expressions);

			// Assert
			Assert.AreEqual(2, result.Length);
			Assert.AreEqual("test1", result[0]);
			Assert.AreEqual("test2", result[1]);
		}

		[TestMethod]
		public void ExpandArray_WithMatchingAliases_ReturnsExpandedExpressions()
		{
			// Arrange
			var aliases = new Dictionary<string, string>
			{
				{ "#Critical", @"(?<State>Critical)" },
				{ "#Error", @"(?<State>Error)" }
			};
			var expander = new FilterAliasExpander(aliases);
			var expressions = new[] { "#Critical", "#Error" };

			// Act
			var result = expander.Expand(expressions);

			// Assert
			Assert.AreEqual(2, result.Length);
			Assert.AreEqual(@"(?<State>Critical)", result[0]);
			Assert.AreEqual(@"(?<State>Error)", result[1]);
		}

		[TestMethod]
		public void Expand_AliasMatchingIsCaseInsensitive()
		{
			// Arrange
			var aliases = new Dictionary<string, string>
			{
				{ "#Critical", @"(?<State>Critical)" }
			};
			var expander = new FilterAliasExpander(aliases);

			// Act
			var result = expander.Expand("#CRITICAL");

			// Assert
			Assert.AreEqual(@"(?<State>Critical)", result);
		}
	}
}
