namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class DetectDataAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;
		private readonly IFilterAliasExpander _aliasExpander;

		public DetectDataAnalyzer(FilterStrategy filterStrategy)
			: this(filterStrategy, null)
		{
		}

		public DetectDataAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander)
		{
			_filterStrategy = filterStrategy;
			_aliasExpander = aliasExpander;
		}

		public string Key => AnalysisType.DetectData.ToString();

		public string DisplayName => "Detect Data";

		/// <summary>
		/// Extracts key/value pairs defined by regular expression "groups", and then updates the corresponding <see cref="Metadata.Comment"/>.
		/// </summary>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions">MSDN: Defining RegEx Groups</see>
		public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var count = 0;

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
				if (canUpdateMetadata)
				{
					record.Metadata.IsFlagged = false;
				}

				// Track unique key/value pairs per record to avoid duplicates using tuple-based key
				var seenKeyValues = new HashSet<(string Key, string Value)>();

				foreach (RegularExpression regexExpression in expressions)
				{
					IDictionary<string, string> keyValuePairs = regexExpression.GetKeyValuePairs(record);

					if (keyValuePairs.Count > 0)
					{
						foreach (KeyValuePair<string, string> keyValuePair in keyValuePairs)
						{
							if (!string.IsNullOrWhiteSpace(keyValuePair.Value))
							{
								var compositeKey = (keyValuePair.Key, keyValuePair.Value);

								// Skip duplicate key/value pairs within the same record
								if (seenKeyValues.Contains(compositeKey))
								{
									continue;
								}

								seenKeyValues.Add(compositeKey);

								var parameterName = RegularExpression.GetFriendlyParameterName(keyValuePair.Key);

								if (canUpdateMetadata)
								{
									record.Metadata.IsFlagged = true;
									record.Metadata.UpdateUserComment($"{parameterName}: {keyValuePair.Value}");
								}

								count++;
							}
						}
					}
				}
			}

			return new Results(count);
		}
	}
}
