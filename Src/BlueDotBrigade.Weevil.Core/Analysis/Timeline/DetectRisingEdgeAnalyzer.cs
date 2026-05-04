namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
      using System.Globalization;
	using System.Linq;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	/// <summary>
	/// Extracts numeric values via regex named capture groups and compares each to the previous value.
	/// Flags records where the numeric value increases compared to the preceding record.
	/// </summary>
	internal class DetectRisingEdgeAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;
		private readonly IFilterAliasExpander _aliasExpander;

		public DetectRisingEdgeAnalyzer(FilterStrategy filterStrategy)
			: this(filterStrategy, null)
		{
		}

		public DetectRisingEdgeAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander)
		{
			_filterStrategy = filterStrategy;
			_aliasExpander = aliasExpander;
		}

		public string Key => AnalysisType.DetectRisingEdges.ToString();

		public string DisplayName => "Detect Rising Edges";


		/// <summary>
		/// Regular expression groups are used to identify transitions (e.g. changing from <see langword="True"/> to <see langword="False"/>).
		/// </summary>
		/// <remarks>
		/// When a transition occurs:
		/// 1. the corresponding record is flagged
		/// 2. an appropriate comment is added to the record
		/// </remarks>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions">MSDN: Defining RegEx Groups</see>
		public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var count = 0;

			// Get default regex from current inclusive filter
			var defaultRegex = AnalysisHelper.GetDefaultRegex(_filterStrategy);

			// Show analysis dialog to get custom regex
			var recordsDescription = records.Length.ToString("N0");

			if (!userDialog.TryGetExpressions(defaultRegex, recordsDescription, out var customRegex))
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

			var analysisOrder = AnalysisHelper.GetAnalysisOrder(userDialog);

			var previous = new Dictionary<string, string>();
			var previousRecord = new Dictionary<string, IRecord>();
			var isInRisingRun = new Dictionary<string, bool>();

			ImmutableArray<IRecord> sortedRecords = analysisOrder == AnalysisOrder.Ascending
					? records
					: records.OrderByDescending((x => x.LineNumber)).ToImmutableArray();

				foreach (IRecord record in sortedRecords)
				{
					AnalysisHelper.ClearRecordFlag(record, canUpdateMetadata);

						foreach (RegularExpression expression in expressions)
						{
							IDictionary<string, string> keyValuePairs = expression.GetKeyValuePairs(record);

							if (keyValuePairs.Count > 0)
							{
								foreach (KeyValuePair<string, string> current in keyValuePairs)
								{
									if (!string.IsNullOrWhiteSpace(current.Value))
									{
                                  if (previous.TryGetValue(current.Key, out var previousRaw) &&
										previousRecord.TryGetValue(current.Key, out var priorRecord) &&
										decimal.TryParse(previousRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var previousValue) &&
										decimal.TryParse(current.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var currentValue))
									{
										var isRising = currentValue > previousValue;
										var wasRising = isInRisingRun.TryGetValue(current.Key, out var priorState) && priorState;

										if (isRising && !wasRising)
										{
											// Flag the record BEFORE the rise (the valley / last stable value).
											// Mirrors DetectFallingEdgeAnalyzer for symmetric semantics.
											var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);

											count++;

											AnalysisHelper.UpdateRecordMetadata(
												priorRecord,
												true,
												$"{parameterName}: {previousRaw} => {current.Value}",
												canUpdateMetadata);
										}

										isInRisingRun[current.Key] = isRising;
										previous[current.Key] = current.Value;
										previousRecord[current.Key] = record;
									}
									else
									{
										previous[current.Key] = current.Value;
										previousRecord[current.Key] = record;
										isInRisingRun[current.Key] = false;
									}
									}
								}
							}
						}
				}

			return new Results(count);
		}
	}
}
