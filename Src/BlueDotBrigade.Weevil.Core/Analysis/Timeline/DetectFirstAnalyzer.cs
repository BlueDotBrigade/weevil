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

		public DetectFirstAnalyzer(FilterStrategy filterStrategy)
		{
			_filterStrategy = filterStrategy;
		}

		public string Key => AnalysisType.DetectFirst.ToString();

		public string DisplayName => "Detect First";

		public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var count = 0;

		// Get default regex from current inclusive filter
		var defaultRegex = string.Empty;
		if (_filterStrategy != FilterStrategy.KeepAllRecords && _filterStrategy.InclusiveFilter.Count > 0)
		{
			defaultRegex = _filterStrategy.FilterCriteria.Include;
		}

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
		if (!expressionBuilder.TryGetExpression(customRegex, out var customExpression))
		{
			return new Results(0);
		}

		if (!(customExpression is RegularExpression customRegexExpression))
		{
			return new Results(0);
		}

		// Use custom regex instead of filter strategy
		var expressions = ImmutableArray.Create(customRegexExpression);

			if (_filterStrategy != FilterStrategy.KeepAllRecords)
			{
				if (_filterStrategy.InclusiveFilter.Count > 0)
				{
					var foundValues = new HashSet<string>();
				// Using custom regex from dialog instead of filter strategy

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
				}
			}

			return new Results(count);
		}
	}
}
