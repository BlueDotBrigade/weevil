namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Statistics;
	using BlueDotBrigade.Weevil.Filter.Expressions;

	internal sealed class StatisticalAnalyzer : IRecordAnalyzer
	{
		/// <summary>
		/// Represents the percentage of data to be trimmed from both ends when calculating the mean average value.
		/// </summary>
		private const double TrimmedMeanPercent = 0.1;

		private readonly IReadOnlyList<ICalculator> _calculators;
		private readonly FilterStrategy _filterStrategy;
		private readonly IFilterAliasExpander _aliasExpander;
		private readonly IDictionary<string, double> _results;

		public StatisticalAnalyzer(FilterStrategy filterStrategy)
			: this(filterStrategy, null)
		{
		}

		public StatisticalAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander)
		{
			_filterStrategy = filterStrategy;
			_aliasExpander = aliasExpander;

			_calculators = new List<ICalculator>
			{
				new CountCalculator(),
				new MinCalculator(),
				new MaxCalculator(),
				new MeanCalculator(),
				new MedianCalculator(),
				new TrimmedMeanCalculator(TrimmedMeanPercent),
				new StandardDeviationCalculator(),
			};

			_results = new Dictionary<string, double>
			{
				{ "Count", 0 },
				{ "Range", 0 },
				{ "Min", 0 },
				{ "Max", 0 },
				{ "Mean", 0 },
				{ "Median", 0 },
				{ "TrimmedMean", 0 },
				{ "StdDev", 0 }
			};
		}

		public string Key => AnalysisType.Statistical.ToString();

		public string DisplayName => "Statistics";

		public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var count = 0;

			var timestamps = new List<DateTime>();
			var values = new List<double>();

			// Get default regex from current inclusive filter
			var defaultRegex = Timeline.AnalysisHelper.GetDefaultRegex(_filterStrategy);

			// Show analysis dialog to get custom regex
			var recordsDescription = records.Length.ToString("N0");

			if (!userDialog.TryShowAnalysisDialog(defaultRegex, recordsDescription, out var customRegex))
			{
				// User cancelled
				return new Results(0);
			}

			if (string.IsNullOrWhiteSpace(customRegex))
			{
				// No regex provided
				return new Results(0);
			}

			// Parse expressions with alias expansion and || support
			var expressionBuilder = _filterStrategy.GetExpressionBuilder();
			ImmutableArray<RegularExpression> expressions = AnalyzerExpressionHelper.ParseExpressions(
				customRegex,
				_aliasExpander,
				expressionBuilder);

			if (expressions.IsDefaultOrEmpty)
			{
				return new Results(0);
			}

			foreach (IRecord record in records)
			{
				// Collect all key-value pairs from all expressions for this record
				// For statistical analysis, we take the first numeric value found
				double? foundValue = null;

				foreach (RegularExpression regexExpression in expressions)
				{
					IDictionary<string, string> keyValuePairs = regexExpression.GetKeyValuePairs(record);

					if (keyValuePairs.Count == 1)
					{
						if (double.TryParse(keyValuePairs.First().Value, out var value))
						{
							foundValue = value;
							break; // Take first numeric value found
						}
					}
					else if (keyValuePairs.Count >= 2)
					{
						throw new MatchCountException(
							"(custom regex)",
							$"The regular expression should have only 1 matching group.");
					}
				}

				if (foundValue.HasValue)
				{
					count++;
					timestamps.Add(record.CreatedAt);
					values.Add(foundValue.Value);

					if (canUpdateMetadata)
					{
						record.Metadata.IsFlagged = false;
					}
				}
			}

			var data = _calculators
				.ToDictionary(c => c.Name, c => (object)c.Calculate(values));

			var rangeCalc = new RangeCalculator();
			data["Range"] = rangeCalc.Calculate(timestamps);

			return new Results(count, data);
		}
	}
}