namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Globalization;
	using System.Threading;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class ThresholdCrossingsAnalyzer : RegularExpressionAnalyzer
	{
		public ThresholdCrossingsAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander = null)
			: base(filterStrategy, aliasExpander)
		{
		}

		public override string Key => AnalysisType.ThresholdCrossings.ToString();

		public override string DisplayName => "Threshold Crossings";

		/// <remarks>
		/// Stops within one record when <paramref name="cancellationToken"/> is cancelled, and keeps the flags and comments already changed (#1027).
		/// </remarks>
		public override Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata, CancellationToken cancellationToken)
		{
			const string defaultThreshold = "0";
			const string defaultComparison = ">";

			// Its own dialog, which also asks for the threshold and the comparison.
			if (!userDialog.TryGetThreshold(
				this.DefaultExpression,
				records.Count.ToString("N0"),
				defaultThreshold,
				defaultComparison,
				out var customRegex,
				out var thresholdInput,
				out var comparisonInput))
			{
				return new Results(0);
			}

			thresholdInput = string.IsNullOrWhiteSpace(thresholdInput)
				? defaultThreshold
				: thresholdInput;

			comparisonInput = string.IsNullOrWhiteSpace(comparisonInput)
				? defaultComparison
				: comparisonInput;

			ImmutableArray<RegularExpression> expressions = ParseExpressions<RegularExpression>(customRegex);

			if (expressions.IsEmpty)
			{
				return new Results(0);
			}

			if (!decimal.TryParse(thresholdInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
			{
				return new Results(0);
			}

			if (!TryGetComparison(comparisonInput, out ThresholdComparison comparison))
			{
				return new Results(0);
			}

			ClearFlags(records, state, canUpdateMetadata);

			var count = 0;

			// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
			var buffer = new ContentBuffer();

			for (var position = 0; position < records.Count; position++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var wasFlagged = false;

				// Read once, however many expressions test the record (#1133).
				var content = records.ReadContent(position, buffer).ToString();

				foreach (RegularExpression expression in expressions)
				{
					IDictionary<string, string> keyValuePairs = expression.GetKeyValuePairs(content);

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
						Flag(
							records,
							position,
							state,
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

		private static bool TryGetComparison(string userInput, out ThresholdComparison comparison)
		{
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
