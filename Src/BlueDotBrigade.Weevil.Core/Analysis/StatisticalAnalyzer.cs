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

			if (_filterStrategy != FilterStrategy.KeepAllRecords)
			{
				// Problem: There can be multiple RegEx, each with 0 or more capturing groups.
				// To simplify the implementation, assume that there is only one RegEx group.

				ImmutableArray<RegularExpression> expressions = _filterStrategy.InclusiveFilter.GetRegularExpressions();
				if (expressions.Length != 1)
				{
					throw new MatchCountException(
						"(include filter)",
						$"The include filter should only have 1 expression.");
				}

				RegularExpression expression = expressions.First();

				foreach (IRecord record in records)
				{
					IDictionary<string, string> keyValuePairs = expression.GetKeyValuePairs(record);

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
							"(include filter)",
							$"The include filter's regular expression should have only 1 matching group.");
					}

				}
			}

			var data = _calculators
				.Select(c => c.Calculate(values, timestamps))
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

			return new Results(count, data);
		}
	}
}