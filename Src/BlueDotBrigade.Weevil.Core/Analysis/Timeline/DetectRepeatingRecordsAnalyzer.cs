namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;

	internal class DetectRepeatingRecordsAnalyzer : IRecordAnalyzer
	{
		private readonly ExpressionBuilder _expressionBuilder;

		public DetectRepeatingRecordsAnalyzer(FilterStrategy filterStrategy)
		{
			_expressionBuilder = filterStrategy.GetExpressionBuilder();
		}

		public string Key => AnalysisType.DetectRepeatingRecords.ToString();

		public string DisplayName => "Detect Both Edges";

		public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
        {
            var flaggedRecords = 0;
			var blockCount = 0;

            // Show analysis dialog to get custom regex
            var recordsDescription = records.Length.ToString("N0");

            if (!userDialog.TryShowAnalysisDialog(string.Empty, recordsDescription, out var customRegex))
            {
                // User cancelled
                return new Results(0);
            }

            if (string.IsNullOrWhiteSpace(customRegex))
            {
                // No regex provided
                return new Results(0);
            }

            if (_expressionBuilder.TryGetExpression(customRegex, out IExpression expression))
            {
                var sortedRecords = records.OrderBy((x => x.LineNumber)).ToImmutableArray();

                IRecord firstMatch = null;
                IRecord lastMatch = null;

				foreach (IRecord record in sortedRecords)
                {
					if (canUpdateMetadata)
					{
						record.Metadata.IsFlagged = false;
					}

					if (expression.IsMatch(record))
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
								// WHAT DO WE DO HERE ???
							}
							else
							{
								Log.Default.Write($"Detected a block of repeating records. StartsAt={firstMatch.LineNumber}, EndsAt={lastMatch.LineNumber}");

								blockCount++;

								if (canUpdateMetadata)
								{
									firstMatch.Metadata.UpdateUserComment($"{blockCount:00}-Begins");
									firstMatch.Metadata.IsFlagged = true;

									lastMatch.Metadata.UpdateUserComment($"{blockCount:00}-Ends");
									lastMatch.Metadata.IsFlagged = true;
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
						firstMatch.Metadata.UpdateUserComment($"{blockCount:00}-Begins");
						firstMatch.Metadata.IsFlagged = true;

						lastMatch.Metadata.UpdateUserComment($"{blockCount:00}-Ends");
						lastMatch.Metadata.IsFlagged = true;
					}

					flaggedRecords += 2;
				}
			}

			return new Results(flaggedRecords);
        }
	}
}
