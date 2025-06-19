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

            var serializedExpression = userDialog.ShowUserPrompt(
            "Detect Edges",
            "Regular Expression:",
            "");

            if (_expressionBuilder.TryGetExpression(serializedExpression, out IExpression expression))
            {
                var sortedRecords = records.OrderBy((x => x.LineNumber)).ToImmutableArray();

                IRecord firstMatch = null;
                IRecord lastMatch = null;

				foreach (IRecord record in sortedRecords)
                {
					record.Metadata.IsFlagged = false;

					// Looking at a record within the target block?
					// ... If not, assume we are at the end of the block & reset.
					if (expression.IsMatch(record))
                    {
                        if (firstMatch == null)
                        {
                            firstMatch = record;
                        }
						else
						{
							// Intent: drag this pointer to the end of the block
							lastMatch = record;
						}
					}
					else
					{
						// Start of block found?
						if (firstMatch != null)
						{
							// Block contains only 1 record?
							if (lastMatch == null)
							{
								// WHAT DO WE DO HERE ???
							}
							else
							{
								Log.Default.Write($"Detected a block of repeating records. StartsAt={firstMatch.LineNumber}, EndsAt={lastMatch.LineNumber}");

								blockCount++;

								firstMatch.Metadata.UpdateUserComment($"{blockCount:00}-Begins");
								firstMatch.Metadata.IsFlagged = true;
								flaggedRecords++;

								lastMatch.Metadata.UpdateUserComment($"{blockCount:00}-Ends");
								lastMatch.Metadata.IsFlagged = true;
								flaggedRecords++;
							}
						}

						// Reset state to begin searching for the next block.
						firstMatch = null;
						lastMatch = null;
					}
                }

				// Is the block at the end of the results?
				if (firstMatch != null && lastMatch != null)
				{
					Log.Default.Write($"Detected the last block of repeating records. StartsAt={firstMatch.LineNumber}, EndsAt={lastMatch.LineNumber}");

					blockCount++;

					firstMatch.Metadata.UpdateUserComment($"{blockCount:00}-Begins");
					firstMatch.Metadata.IsFlagged = true;
					flaggedRecords++;

					lastMatch.Metadata.UpdateUserComment($"{blockCount:00}-Ends");
					lastMatch.Metadata.IsFlagged = true;
					flaggedRecords++;
				}
			}

			return new Results(flaggedRecords);
        }
	}
}
