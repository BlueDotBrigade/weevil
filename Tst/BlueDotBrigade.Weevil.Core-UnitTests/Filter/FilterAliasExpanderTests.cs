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
			(result).Should().Be("test expression");
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
			(result).Should().Be(@"(?<State>Critical|Error)");
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
			(result).Should().Be("#Warning");
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
			(result).Should().Be(@"(?<State>Critical)||(?<State>Error)");
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
			(result).Should().Be(@"(?<State>Critical)||(?<State>Warning)");
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
			(result).Should().Be(string.Empty);
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
			(result).Should().Be(string.Empty);
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
			(result.Length).Should().Be(2);
			(result[0]).Should().Be("test1");
			(result[1]).Should().Be("test2");
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
			(result.Length).Should().Be(2);
			(result[0]).Should().Be(@"(?<State>Critical)");
			(result[1]).Should().Be(@"(?<State>Error)");
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
			(result).Should().Be(@"(?<State>Critical)");
		}
	}
}
