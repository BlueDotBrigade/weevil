namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Data;
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

			// Expand aliases first - this replaces aliases with their full expressions
			// and preserves the || separators in the output
			var expandedInput = aliasExpander?.Expand(rawInput) ?? rawInput;

			// Split by || (uses the same delimiter constant as filtering)
			var segments = expandedInput.Split(
				new[] { Constants.FilterOrOperator },
				StringSplitOptions.RemoveEmptyEntries);

			foreach (var segment in segments)
			{
				var trimmedSegment = segment.Trim();

				if (string.IsNullOrWhiteSpace(trimmedSegment))
				{
					continue;
				}

				if (expressionBuilder != null && expressionBuilder.TryGetExpression(trimmedSegment, out var expression))
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

			// Expand aliases first - this replaces aliases with their full expressions
			// and preserves the || separators in the output
			var expandedInput = aliasExpander?.Expand(rawInput) ?? rawInput;

			// Split by || (uses the same delimiter constant as filtering)
			var segments = expandedInput.Split(
				new[] { Constants.FilterOrOperator },
				StringSplitOptions.RemoveEmptyEntries);

			foreach (var segment in segments)
			{
				var trimmedSegment = segment.Trim();

				if (string.IsNullOrWhiteSpace(trimmedSegment))
				{
					continue;
				}

				if (expressionBuilder != null && expressionBuilder.TryGetExpression(trimmedSegment, out var expression))
				{
					results.Add(expression);
				}
			}

			return results.ToImmutableArray();
		}

		/// <summary>
		/// Collects non-empty named-group values for a single record across multiple expressions.
		/// Conflicting values for the same key are treated as ambiguous and ignored for that record.
		/// </summary>
		/// <param name="expressions">The expressions to evaluate.</param>
		/// <param name="record">The record being analyzed.</param>
		/// <returns>A dictionary containing at most one resolved value per key for the record.</returns>
		public static Dictionary<string, string> GetResolvedKeyValuePairs(
			ImmutableArray<RegularExpression> expressions,
			IRecord record)
		{
			var resolvedValues = new Dictionary<string, string>();
			var ambiguousKeys = new HashSet<string>();

			foreach (RegularExpression expression in expressions)
			{
				IDictionary<string, string> keyValuePairs = expression.GetKeyValuePairs(record);

				if (keyValuePairs.Count == 0)
				{
					continue;
				}

				foreach (KeyValuePair<string, string> currentState in keyValuePairs)
				{
					if (string.IsNullOrWhiteSpace(currentState.Value) || ambiguousKeys.Contains(currentState.Key))
					{
						continue;
					}

					if (resolvedValues.TryGetValue(currentState.Key, out var existingValue))
					{
						if (existingValue != currentState.Value)
						{
							resolvedValues.Remove(currentState.Key);
							ambiguousKeys.Add(currentState.Key);
						}
					}
					else
					{
						resolvedValues.Add(currentState.Key, currentState.Value);
					}
				}
			}

			return resolvedValues;
		}
	}
}
