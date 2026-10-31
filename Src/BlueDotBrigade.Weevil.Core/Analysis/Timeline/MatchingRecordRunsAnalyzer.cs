namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;

	/// <summary>
	/// Evaluates records as match/no-match and identifies maximal contiguous runs of matching records.
	/// Flags the first and last records in each run containing two or more matches.
	/// A non-matching record terminates the current run.
	/// </summary>
	internal class MatchingRecordRunsAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;
		private readonly IFilterAliasExpander _aliasExpander;

		public MatchingRecordRunsAnalyzer(FilterStrategy filterStrategy)
			: this(filterStrategy, null)
		{
		}

		public MatchingRecordRunsAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander)
		{
			_filterStrategy = filterStrategy;
			_aliasExpander = aliasExpander;
		}

		public string Key => AnalysisType.MatchingRecordRuns.ToString();

		public string DisplayName => "Matching Record Runs";

		public Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
        {
            var flaggedRecords = 0;
			var blockCount = 0;

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
            var expressionBuilder = _filterStrategy.GetAnalyzerExpressionBuilder();
            ImmutableArray<IExpression> expressions = AnalyzerExpressionHelper.ParseAllExpressions(
                customRegex,
                _aliasExpander,
                expressionBuilder);

            if (!expressions.IsDefaultOrEmpty)
            {
                IRecord firstMatch = null;
                IRecord lastMatch = null;
                var content = new ContentBuffer();

				// The filter results and the selection are already in line number order, so they are not sorted into a copy (#1137).
				foreach (IRecord record in records)
                {
					if (canUpdateMetadata)
					{
						state.SetFlagged(record, false);
					}

					// Check if any expression matches
					var isMatch = false;
					foreach (IExpression expression in expressions)
					{
						if (expression.IsMatch(record, content))
						{
							isMatch = true;
							break;
						}
					}

					if (isMatch)
                    {
                        if (firstMatch == null)
                        {
                            firstMatch = record;
                        }
						else
						{
							lastMatch = record;
						}
					}
					else
					{
						if (firstMatch != null)
						{
							if (lastMatch == null)
							{
								// Single-record matches are ignored because this analyzer only flags
								// runs containing at least two consecutive matching records.
							}
							else
							{
								Log.Default.Write($"Detected a block of repeating records. StartsAt={firstMatch.LineNumber}, EndsAt={lastMatch.LineNumber}");

								blockCount++;

								if (canUpdateMetadata)
								{
									state.AppendComment(firstMatch, $"{blockCount:00}-Begins");
									state.SetFlagged(firstMatch, true);

									state.AppendComment(lastMatch, $"{blockCount:00}-Ends");
									state.SetFlagged(lastMatch, true);
								}

								flaggedRecords += 2;
							}
						}

						firstMatch = null;
						lastMatch = null;
					}
                }

				if (firstMatch != null && lastMatch != null)
				{
					Log.Default.Write($"Detected the last block of repeating records. StartsAt={firstMatch.LineNumber}, EndsAt={lastMatch.LineNumber}");

					blockCount++;

					if (canUpdateMetadata)
					{
						state.AppendComment(firstMatch, $"{blockCount:00}-Begins");
						state.SetFlagged(firstMatch, true);

						state.AppendComment(lastMatch, $"{blockCount:00}-Ends");
						state.SetFlagged(lastMatch, true);
					}

					flaggedRecords += 2;
				}
			}

			return new Results(flaggedRecords);
        }
	}
}
