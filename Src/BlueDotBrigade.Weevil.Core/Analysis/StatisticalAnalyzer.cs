namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Globalization;
	using System.Linq;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Math;

	internal sealed class StatisticalAnalyzer : RegularExpressionAnalyzer
	{
		/// <summary>
		/// Represents the percentage of data to be trimmed from both ends when calculating the mean average value.
		/// </summary>
		private const double TrimmedMeanPercent = 0.1;

		private readonly IReadOnlyList<ICalculator> _calculators;
		private readonly IDictionary<string, double> _results;

		public StatisticalAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander = null)
			: base(filterStrategy, aliasExpander)
		{
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

		public override string Key => AnalysisType.Statistical.ToString();

		public override string DisplayName => "Statistics";

		public override Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata, CancellationToken cancellationToken)
		{
			if (!TryGetExpressions(records, userDialog, out ImmutableArray<RegularExpression> expressions))
			{
				return new Results(0);
			}

			ClearFlags(records, state, canUpdateMetadata);

			var timestamps = new List<DateTime>();
			var values = new List<double>();

			// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
			var buffer = new ContentBuffer();

			for (var position = 0; position < records.Count; position++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				// Collect all key-value pairs from all expressions for this record
				// For statistical analysis, we take the first numeric value found
				double? foundValue = null;

				// Read once, however many expressions test the record (#1133).
				var content = records.ReadContent(position, buffer).ToString();

				foreach (RegularExpression regexExpression in expressions)
				{
					IDictionary<string, string> keyValuePairs = regexExpression.GetKeyValuePairs(content);

					if (keyValuePairs.Count == 1)
					{
						if (double.TryParse(
							keyValuePairs.First().Value,
							NumberStyles.Float,
							CultureInfo.InvariantCulture,
							out var value))
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
					DateTime createdAt = records.GetCreatedAt(position);

					if (createdAt != Record.CreationTimeUnknown)
					{
						timestamps.Add(createdAt);
					}
					values.Add(foundValue.Value);
				}
			}

			var data = _calculators
				.ToDictionary(c => c.Name, c => (object)c.Calculate(values));

			var rangeCalc = new RangeCalculator();
			data["Range"] = rangeCalc.Calculate(timestamps);

			return new Results(0, data);
		}
	}
}