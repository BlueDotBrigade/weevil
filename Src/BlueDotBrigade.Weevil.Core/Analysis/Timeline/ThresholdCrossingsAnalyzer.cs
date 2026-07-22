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

			var expressionBuilder = _filterStrategy.GetExpressionBuilder();
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

			if (!TryGetDirection(userDialog, out var direction))
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

						var doesCrossThreshold = direction == ThresholdDirection.Above
							? value > threshold
							: value < threshold;

						if (!doesCrossThreshold)
						{
							continue;
						}

						var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);
						var comparison = direction == ThresholdDirection.Above ? ">" : "<";

						AnalysisHelper.UpdateRecordMetadata(
							record,
							true,
							$"{parameterName}: {current.Value} {comparison} {threshold.ToString(CultureInfo.InvariantCulture)}",
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

		private static bool TryGetDirection(IUserDialog userDialog, out ThresholdDirection direction)
		{
			var userInput = userDialog.ShowUserPrompt(
				"Threshold Crossings",
				"Direction (Above/Below):",
				"Above");

			return Enum.TryParse(userInput, true, out direction);
		}

		private enum ThresholdDirection
		{
			Above,
			Below
		}
	}
}
