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
		private readonly IDictionary<string, double> _results;

		public StatisticalAnalyzer(FilterStrategy filterStrategy)
		{
			_filterStrategy = filterStrategy;

			_calculators = new List<ICalculator>
			{
				new RangeCalculator(),
				new CountCalculator(),
				new MinCalculator(),
				new MaxCalculator(),
				new MeanCalculator(),
				new MedianCalculator(),
				new TrimmedMeanCalculator(TrimmedMeanPercent),
			};

			_results = new Dictionary<string, double>
			{
				{ "Count", 0 },
				{ "Range", 0 },
				{ "Min", 0 },
				{ "Max", 0 },
				{ "Mean", 0 },
				{ "Median", 0 },
				{ "TrimmedMean", 0 }
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

			// Create expression from custom regex
			var expressionBuilder = _filterStrategy.GetExpressionBuilder();
			if (!expressionBuilder.TryGetExpression(customRegex, out var expression))
			{
				return new Results(0);
			}

			if (!(expression is RegularExpression regexExpression))
			{
				return new Results(0);
			}

			foreach (IRecord record in records)
			{
				IDictionary<string, string> keyValuePairs = regexExpression.GetKeyValuePairs(record);

				if (keyValuePairs.Count == 1)
				{
					if (double.TryParse(keyValuePairs.First().Value, out var value))
					{
						count++;

						timestamps.Add(record.CreatedAt);
						values.Add(value);

						if (canUpdateMetadata)
						{
							record.Metadata.IsFlagged = false;
						}
					}
				}
				else if (keyValuePairs.Count >= 2)
				{
					throw new MatchCountException(
						"(custom regex)",
						$"The regular expression should have only 1 matching group.");
				}
			}

			var data = _calculators
				.Select(c => c.Calculate(values, timestamps))
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

			return new Results(count, data);
		}
	}
}