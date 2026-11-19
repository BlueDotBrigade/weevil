namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
	using System.Threading;
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
	internal class MatchingRecordRunsAnalyzer : RegularExpressionAnalyzer
	{
		public MatchingRecordRunsAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander = null)
			: base(filterStrategy, aliasExpander)
		{
		}

		public override string Key => AnalysisType.MatchingRecordRuns.ToString();

		public override string DisplayName => "Matching Record Runs";

		/// <remarks>
		/// Stops within one record when <paramref name="cancellationToken"/> is cancelled, and keeps the flags and comments already changed (#1027).
		/// </remarks>
		public override Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata, CancellationToken cancellationToken)
		{
			// Any expression, including a moniker such as @Pinned, not only a regular expression.
			if (!TryGetExpressions(records, userDialog, out ImmutableArray<IExpression> expressions))
			{
				return new Results(0);
			}

			ClearFlags(records, state, canUpdateMetadata);

			var flaggedRecords = 0;
			var blockCount = 0;

			IRecord firstMatch = null;
			IRecord lastMatch = null;
			var content = new ContentBuffer();

			// IExpression.IsMatch takes a record, and a moniker reads the record rather than its text,
			// so unlike the other analyzers this one creates a record for every row it tests (#1258).
			// The filter results and the selection are already in line number order, so they are not sorted into a copy (#1137).
			foreach (IRecord record in records)
			{
				cancellationToken.ThrowIfCancellationRequested();

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
					// Single-record matches are ignored because this analyzer only flags
					// runs containing at least two consecutive matching records.
					if (firstMatch != null && lastMatch != null)
					{
						Log.Default.Write($"Detected a block of repeating records. StartsAt={firstMatch.LineNumber}, EndsAt={lastMatch.LineNumber}");

						FlagBlock();
					}

					firstMatch = null;
					lastMatch = null;
				}
			}

			if (firstMatch != null && lastMatch != null)
			{
				Log.Default.Write($"Detected the last block of repeating records. StartsAt={firstMatch.LineNumber}, EndsAt={lastMatch.LineNumber}");

				FlagBlock();
			}

			return new Results(flaggedRecords);

			void FlagBlock()
			{
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
	}
}
