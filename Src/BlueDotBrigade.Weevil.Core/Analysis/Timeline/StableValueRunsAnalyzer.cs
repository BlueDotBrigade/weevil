namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Threading;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	/// <summary>
	/// Parses regex named capture groups and tracks each captured key independently across records.
	/// Flags the first and last records in every maximal contiguous run with the same captured value.
	/// A changed value finalizes the current run and starts a new one; a non-matching record finalizes
	/// the current run without starting another.
	/// </summary>
	internal class StableValueRunsAnalyzer : RegularExpressionAnalyzer
	{
		public StableValueRunsAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander = null)
			: base(filterStrategy, aliasExpander)
		{
		}

		public override string Key => AnalysisType.StableValueRuns.ToString();

		public override string DisplayName => "Stable Value Runs";

		/// <remarks>
		/// Stops within one record when <paramref name="cancellationToken"/> is cancelled, and keeps the flags and comments already changed (#1027).
		/// </remarks>
		public override Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata, CancellationToken cancellationToken)
		{
			if (!TryGetExpressions(records, userDialog, out ImmutableArray<RegularExpression> expressions))
			{
				return new Results(0);
			}

			ClearFlags(records, state, canUpdateMetadata);

			var flaggedLineNumbers = new HashSet<int>();
			var activeRuns = new Dictionary<string, ValueRun>();

			// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
			var buffer = new ContentBuffer();

			for (var position = 0; position < records.Count; position++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var matchedKeys = new HashSet<string>();
				var content = records.ReadContent(position, buffer).ToString();

				foreach (KeyValuePair<string, string> currentState in AnalyzerExpressionHelper.GetResolvedKeyValuePairs(expressions, content))
				{
					matchedKeys.Add(currentState.Key);

					if (activeRuns.TryGetValue(currentState.Key, out ValueRun existingRun))
					{
						if (existingRun.Value == currentState.Value)
						{
							existingRun.LastPosition = position;
						}
						else
						{
							FinalizeRun(currentState.Key, existingRun);
							StartNewRun(currentState.Key, currentState.Value, position);
						}
					}
					else
					{
						StartNewRun(currentState.Key, currentState.Value, position);
					}
				}

				if (activeRuns.Count > 0)
				{
					List<string> keysToFinalize = activeRuns.Keys
						.Where(key => !matchedKeys.Contains(key))
						.ToList();

					foreach (string key in keysToFinalize)
					{
						if (activeRuns.TryGetValue(key, out ValueRun run))
						{
							FinalizeRun(key, run);
						}
					}
				}
			}

			foreach (KeyValuePair<string, ValueRun> activeRun in activeRuns.ToArray())
			{
				FinalizeRun(activeRun.Key, activeRun.Value);
			}

			return new Results(flaggedLineNumbers.Count);

			void StartNewRun(string key, string value, int position)
			{
				var friendlyName = RegularExpression.GetFriendlyParameterName(key);
				activeRuns[key] = new ValueRun(friendlyName, value, position);

				flaggedLineNumbers.Add(records.GetLineNumber(position));
				Flag(records, position, state, $"Start {friendlyName}: {value}", canUpdateMetadata);
			}

			void FinalizeRun(string key, ValueRun run)
			{
				flaggedLineNumbers.Add(records.GetLineNumber(run.LastPosition));
				Flag(records, run.LastPosition, state, $"Stop {run.FriendlyName}: {run.Value}", canUpdateMetadata);

				activeRuns.Remove(key);
			}
		}

		private sealed class ValueRun
		{
			public ValueRun(string friendlyName, string value, int position)
			{
				this.FriendlyName = friendlyName;
				this.Value = value;
				this.LastPosition = position;
			}

			public string FriendlyName { get; }

			public string Value { get; }

			/// <summary>
			/// The run's last row, whose record is created only when the run ends.
			/// </summary>
			public int LastPosition { get; set; }
		}
	}
}
