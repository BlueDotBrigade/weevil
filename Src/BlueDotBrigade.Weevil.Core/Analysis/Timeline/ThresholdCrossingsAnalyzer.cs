namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Globalization;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class ThresholdCrossingsAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;
		private readonly IFilterAliasExpander _aliasExpander;

		public ThresholdCrossingsAnalyzer(FilterStrategy filterStrategy)
			: this(filterStrategy, null)
		{
		}

		public ThresholdCrossingsAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander)
		{
			_filterStrategy = filterStrategy;
			_aliasExpander = aliasExpander;
		}

		public string Key => AnalysisType.ThresholdCrossings.ToString();

		public string DisplayName => "Threshold Crossings";

		public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var count = 0;
			var defaultRegex = AnalysisHelper.GetDefaultRegex(_filterStrategy);
			var recordsDescription = records.Length.ToString("N0");

			if (!userDialog.TryGetExpressions(defaultRegex, recordsDescription, out var customRegex))
			{
				return new Results(0);
			}

			if (string.IsNullOrWhiteSpace(customRegex))
			{
				return new Results(0);
			}

			var expressionBuilder = _filterStrategy.GetAnalyzerExpressionBuilder();
			ImmutableArray<RegularExpression> expressions = AnalyzerExpressionHelper.ParseExpressions(
				customRegex,
				_aliasExpander,
				expressionBuilder);

			if (expressions.IsDefaultOrEmpty)
			{
				return new Results(0);
			}

			if (!TryGetThreshold(userDialog, out var threshold))
			{
				return new Results(0);
			}

			if (!TryGetComparison(userDialog, out var comparison))
			{
				return new Results(0);
			}

			foreach (IRecord record in records)
			{
				AnalysisHelper.ClearRecordFlag(record, canUpdateMetadata);
				var wasFlagged = false;

				foreach (RegularExpression expression in expressions)
				{
					IDictionary<string, string> keyValuePairs = expression.GetKeyValuePairs(record);

					foreach (KeyValuePair<string, string> current in keyValuePairs)
					{
						if (string.IsNullOrWhiteSpace(current.Value))
						{
							continue;
						}

						if (!decimal.TryParse(current.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
						{
							continue;
						}

						var doesCrossThreshold = DoesCrossThreshold(value, threshold, comparison);

						if (!doesCrossThreshold)
						{
							continue;
						}

						var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);
						AnalysisHelper.UpdateRecordMetadata(
							record,
							true,
							$"{parameterName}: {current.Value} {GetComparisonSymbol(comparison)} {threshold.ToString(CultureInfo.InvariantCulture)}",
							canUpdateMetadata);

						if (!wasFlagged)
						{
							count++;
							wasFlagged = true;
						}
					}
				}
			}

			return new Results(count);
		}

		private static bool TryGetThreshold(IUserDialog userDialog, out decimal threshold)
		{
			var userInput = userDialog.ShowUserPrompt(
				"Threshold Crossings",
				"Threshold value:",
				"0");

			return decimal.TryParse(userInput, NumberStyles.Float, CultureInfo.InvariantCulture, out threshold);
		}

		private static bool TryGetComparison(IUserDialog userDialog, out ThresholdComparison comparison)
		{
			var userInput = userDialog.ShowUserPrompt(
				"Threshold Crossings",
				"Comparison (>, >=, <, <=):",
				">");

			if (TryMapComparison(userInput, out comparison))
			{
				return true;
			}

			return Enum.TryParse(userInput, true, out comparison);
		}

		private static bool TryMapComparison(string input, out ThresholdComparison comparison)
		{
			// Keep legacy aliases for compatibility with older scripts/tests that used
			// Above/Below naming before explicit operator-based input was introduced.
			switch ((input ?? string.Empty).Trim())
			{
				case ">":
				case "Above":
				case "GreaterThan":
					comparison = ThresholdComparison.GreaterThan;
					return true;
				case ">=":
				case "AboveOrEqual":
				case "GreaterThanOrEqual":
					comparison = ThresholdComparison.GreaterThanOrEqual;
					return true;
				case "<":
				case "Below":
				case "LessThan":
					comparison = ThresholdComparison.LessThan;
					return true;
				case "<=":
				case "BelowOrEqual":
				case "LessThanOrEqual":
					comparison = ThresholdComparison.LessThanOrEqual;
					return true;
				default:
					comparison = default;
					return false;
			}
		}

		private static bool DoesCrossThreshold(decimal value, decimal threshold, ThresholdComparison comparison)
		{
			switch (comparison)
			{
				case ThresholdComparison.GreaterThan:
					return value > threshold;
				case ThresholdComparison.GreaterThanOrEqual:
					return value >= threshold;
				case ThresholdComparison.LessThan:
					return value < threshold;
				case ThresholdComparison.LessThanOrEqual:
					return value <= threshold;
				default:
					return false;
			}
		}

		private static string GetComparisonSymbol(ThresholdComparison comparison)
		{
			switch (comparison)
			{
				case ThresholdComparison.GreaterThan:
					return ">";
				case ThresholdComparison.GreaterThanOrEqual:
					return ">=";
				case ThresholdComparison.LessThan:
					return "<";
				case ThresholdComparison.LessThanOrEqual:
					return "<=";
				default:
					return "?";
			}
		}

		private enum ThresholdComparison
		{
			GreaterThan,
			GreaterThanOrEqual,
			LessThan,
			LessThanOrEqual
		}
	}
}
