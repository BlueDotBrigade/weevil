namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class DetectFallingEdgeAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;

		public DetectFallingEdgeAnalyzer(FilterStrategy filterStrategy)
		{
			_filterStrategy = filterStrategy;
		}

		public string Key => AnalysisType.DetectFallingEdges.ToString();

		public string DisplayName => "Detect Falling Edges";

		private static AnalysisOrder GetAnalysisOrder(IUserDialog userDialog)
		{
			var userInput = userDialog.ShowUserPrompt(
				"Analysis Details",
				"Analysis order (Ascending/Descending):",
				"Ascending");

			if (Enum.TryParse(userInput, true, out AnalysisOrder direction))
			{
				return direction;
			}

			throw new ArgumentOutOfRangeException(
				$"{nameof(direction)}",
				direction,
				"Unable to perform operation. The analysis order was expected to be either: Ascending or Descending");
		}

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

			var analysisOrder = GetAnalysisOrder(userDialog);

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
					var previous = new Dictionary<string, string>();
		ImmutableArray<RegularExpression> expressions = ImmutableArray.Create(customRegexExpression);

					var sortedRecords = analysisOrder == AnalysisOrder.Ascending
						? records
						: records.OrderByDescending((x => x.LineNumber)).ToImmutableArray();

					foreach (IRecord record in sortedRecords)
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
								foreach (KeyValuePair<string, string> current in keyValuePairs)
								{
									if (!string.IsNullOrWhiteSpace(current.Value))
									{
										if (previous.ContainsKey(current.Key))
										{
											if (long.TryParse(previous[current.Key], out var previousValue) &&
												long.TryParse(current.Value, out var currentValue))
											{
												if (currentValue < previousValue)
												{
													var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);

													count++;

													if (canUpdateMetadata)
													{
														record.Metadata.IsFlagged = true;
														record.Metadata.UpdateUserComment($"{parameterName}: {previous[current.Key]} => {current.Value}");
													}
												}
												previous[current.Key] = current.Value;
											}
										}
										else
										{
											var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);

											count++;

											if (canUpdateMetadata)
											{
												record.Metadata.IsFlagged = true;
												record.Metadata.UpdateUserComment($"{parameterName}: {current.Value}");
											}
											
											previous.Add(current.Key, current.Value);
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
