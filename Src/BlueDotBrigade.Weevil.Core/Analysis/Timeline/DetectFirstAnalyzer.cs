namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class DetectFirstAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;
		private readonly IFilterAliasExpander _aliasExpander;

		public DetectFirstAnalyzer(FilterStrategy filterStrategy)
			: this(filterStrategy, null)
		{
		}

		public DetectFirstAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander)
		{
			_filterStrategy = filterStrategy;
			_aliasExpander = aliasExpander;
		}

		public string Key => AnalysisType.DetectFirst.ToString();

		public string DisplayName => "Detect First";

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

			var foundValues = new HashSet<string>();

			foreach (IRecord record in records)
			{
				if (canUpdateMetadata)
				{
					record.Metadata.IsFlagged = false;
				}

				foreach (RegularExpression expression in expressions)
				{
					IDictionary<string, string> keyValuePairs = expression.GetKeyValuePairs(record);

					if (keyValuePairs.Count > 0)
					{
						foreach (KeyValuePair<string, string> pair in keyValuePairs)
						{
							if (!string.IsNullOrWhiteSpace(pair.Value))
							{
								var uniqueKey = $"{pair.Key}:{pair.Value}";
								if (!foundValues.Contains(uniqueKey))
								{
									var parameterName = RegularExpression.GetFriendlyParameterName(pair.Key);

									if (canUpdateMetadata)
									{
										record.Metadata.IsFlagged = true;
										record.Metadata.UpdateUserComment($"{parameterName}: {pair.Value}");
									}

									foundValues.Add(uniqueKey);
									count++;
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
