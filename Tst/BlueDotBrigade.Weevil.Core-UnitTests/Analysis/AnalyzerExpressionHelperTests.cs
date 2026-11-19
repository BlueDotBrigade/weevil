namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions;
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
		public void ParseAllExpressions_WithNullInput_ReturnsEmptyArray()
		{
			// Arrange
			IFilterAliasExpander aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			// Act
			ImmutableArray<IExpression> result = AnalyzerExpressionHelper.ParseAllExpressions(
				null,
				aliasExpander,
				null);

			// Assert
			result.IsEmpty.Should().BeTrue();
		}

		[TestMethod]
		public void ParseAllExpressions_WithEmptyInput_ReturnsEmptyArray()
		{
			// Arrange
			IFilterAliasExpander aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			// Act
			ImmutableArray<IExpression> result = AnalyzerExpressionHelper.ParseAllExpressions(
				string.Empty,
				aliasExpander,
				null);

			// Assert
			result.IsEmpty.Should().BeTrue();
		}

		[TestMethod]
		public void ParseAllExpressions_WithWhitespaceInput_ReturnsEmptyArray()
		{
			// Arrange
			IFilterAliasExpander aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			// Act
			ImmutableArray<IExpression> result = AnalyzerExpressionHelper.ParseAllExpressions(
				"   ",
				aliasExpander,
				null);

			// Assert
			result.IsEmpty.Should().BeTrue();
		}

		[TestMethod]
		public void GivenExpressionText_WhenParseAllExpressionsUsesNullBuilder_ThenReturnsEmptyArray()
		{
			// Regression: Issue #923
			IFilterAliasExpander aliasExpander = GetMockAliasExpander(new Dictionary<string, string>());

			ImmutableArray<IExpression> result = AnalyzerExpressionHelper.ParseAllExpressions(
				"Value=.*",
				aliasExpander,
				null);

			result.IsEmpty.Should().BeTrue();
		}

		[TestMethod]
		public void GivenEqualValuesForOneKeyAcrossExpressions_WhenValuesAreResolved_ThenOneValueIsReturned()
		{
			// Regression coverage: Issue #926
			var expressions = ImmutableArray.Create(
				new RegularExpression(@"A=(?<Value>\d+)"),
				new RegularExpression(@"B=(?<Value>\d+)"));
			Dictionary<string, string> result = AnalyzerExpressionHelper.GetResolvedKeyValuePairs(expressions, "A=1 B=1");

			result.Should().ContainSingle();
			result["Value"].Should().Be("1");
		}

		[TestMethod]
		public void GivenConflictingValuesForOneKeyAcrossExpressions_WhenValuesAreResolved_ThenKeyIsOmitted()
		{
			// Regression coverage: Issue #926
			var expressions = ImmutableArray.Create(
				new RegularExpression(@"A=(?<Value>\d+)"),
				new RegularExpression(@"B=(?<Value>\d+)"));
			Dictionary<string, string> result = AnalyzerExpressionHelper.GetResolvedKeyValuePairs(expressions, "A=1 B=2");

			result.Should().NotContainKey("Value");
		}
	}
}
