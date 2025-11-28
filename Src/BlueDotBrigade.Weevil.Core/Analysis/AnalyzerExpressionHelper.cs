namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;

	/// <summary>
	/// Provides helper methods for parsing and expanding expressions used by analyzers.
	/// </summary>
	internal static class AnalyzerExpressionHelper
	{
		/// <summary>
		/// Parses a raw input string containing one or more regular expressions (optionally using aliases),
		/// and returns an array of <see cref="RegularExpression"/> objects.
		/// </summary>
		/// <param name="rawInput">The raw input string which may contain aliases (prefixed with #) and/or regular expressions separated by ||.</param>
		/// <param name="aliasExpander">The alias expander used to expand aliases into their full expressions.</param>
		/// <param name="expressionBuilder">The expression builder used to parse expressions.</param>
		/// <returns>An immutable array of <see cref="RegularExpression"/> objects parsed from the input.</returns>
		public static ImmutableArray<RegularExpression> ParseExpressions(
			string rawInput,
			IFilterAliasExpander aliasExpander,
			ExpressionBuilder expressionBuilder)
		{
			var results = new List<RegularExpression>();

			if (string.IsNullOrWhiteSpace(rawInput))
			{
				return ImmutableArray<RegularExpression>.Empty;
			}

			// Expand aliases first (handles the || splitting internally)
			var expandedInput = aliasExpander?.Expand(rawInput) ?? rawInput;

			// Split by || (same delimiter used in filtering)
			var segments = expandedInput.Split(
				FilterStrategy.ExpressionDelimiter,
				StringSplitOptions.RemoveEmptyEntries);

			foreach (var segment in segments)
			{
				var trimmedSegment = segment.Trim();

				if (string.IsNullOrWhiteSpace(trimmedSegment))
				{
					continue;
				}

				if (expressionBuilder.TryGetExpression(trimmedSegment, out var expression))
				{
					if (expression is RegularExpression regexExpression)
					{
						results.Add(regexExpression);
					}
				}
			}

			return results.ToImmutableArray();
		}

		/// <summary>
		/// Parses a raw input string and returns all parsed expressions (not just RegularExpression).
		/// </summary>
		/// <param name="rawInput">The raw input string which may contain aliases and/or expressions separated by ||.</param>
		/// <param name="aliasExpander">The alias expander used to expand aliases into their full expressions.</param>
		/// <param name="expressionBuilder">The expression builder used to parse expressions.</param>
		/// <returns>An immutable array of <see cref="IExpression"/> objects parsed from the input.</returns>
		public static ImmutableArray<IExpression> ParseAllExpressions(
			string rawInput,
			IFilterAliasExpander aliasExpander,
			ExpressionBuilder expressionBuilder)
		{
			var results = new List<IExpression>();

			if (string.IsNullOrWhiteSpace(rawInput))
			{
				return ImmutableArray<IExpression>.Empty;
			}

			// Expand aliases first (handles the || splitting internally)
			var expandedInput = aliasExpander?.Expand(rawInput) ?? rawInput;

			// Split by || (same delimiter used in filtering)
			var segments = expandedInput.Split(
				FilterStrategy.ExpressionDelimiter,
				StringSplitOptions.RemoveEmptyEntries);

			foreach (var segment in segments)
			{
				var trimmedSegment = segment.Trim();

				if (string.IsNullOrWhiteSpace(trimmedSegment))
				{
					continue;
				}

				if (expressionBuilder.TryGetExpression(trimmedSegment, out var expression))
				{
					results.Add(expression);
				}
			}

			return results.ToImmutableArray();
		}
	}
}
