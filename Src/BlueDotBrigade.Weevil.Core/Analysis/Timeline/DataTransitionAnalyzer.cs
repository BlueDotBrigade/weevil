namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class DataTransitionAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;

		public DataTransitionAnalyzer(FilterStrategy filterStrategy)
		{
			_filterStrategy = filterStrategy;
		}

		public string Key => AnalysisType.DetectDataTransition.ToString();

		public string DisplayName => "Detect Data Transitions";

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

			// Use custom regex
			ImmutableArray<RegularExpression> expressions = ImmutableArray.Create(customRegexExpression);

			var previousState = new Dictionary<string, string>();

			foreach (IRecord record in records)
				{
					AnalysisHelper.ClearRecordFlag(record, canUpdateMetadata);

						foreach (RegularExpression expression in expressions)
						{
							IDictionary<string, string> keyValuePairs = expression.GetKeyValuePairs(record);

							if (keyValuePairs.Count > 0)
							{
								foreach (KeyValuePair<string, string> currentState in keyValuePairs)
								{
									if (!string.IsNullOrWhiteSpace(currentState.Value))
									{
										if (previousState.ContainsKey(currentState.Key))
										{
											if (previousState[currentState.Key] != currentState.Value)
											{
												var parameterName = RegularExpression.GetFriendlyParameterName(currentState.Key);

												count++;

												AnalysisHelper.UpdateRecordMetadata(
													record,
													true,
													$"{parameterName}: {currentState.Value}",
													canUpdateMetadata);

												previousState[currentState.Key] = currentState.Value;
											}
										}
										else
										{
											var parameterName = RegularExpression.GetFriendlyParameterName(currentState.Key);

											count++;

											AnalysisHelper.UpdateRecordMetadata(
												record,
												true,
												$"{parameterName}: {currentState.Value}",
												canUpdateMetadata);

											previousState.Add(currentState.Key, currentState.Value);
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
