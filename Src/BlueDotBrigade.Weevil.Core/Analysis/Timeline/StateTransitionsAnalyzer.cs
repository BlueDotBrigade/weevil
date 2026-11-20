namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Threading;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	/// <summary>
	/// Parses regex named capture groups and tracks the previous matching value for each captured key.
	/// Flags the record where a value first appears or changes from that previous matching value.
	/// Records that do not capture a key are ignored and do not reset its previous value.
	/// </summary>
	internal class StateTransitionsAnalyzer : RegularExpressionAnalyzer
	{
		public StateTransitionsAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander = null)
			: base(filterStrategy, aliasExpander)
		{
		}

		public override string Key => AnalysisType.StateTransitions.ToString();

		public override string DisplayName => "State Transitions";

		/// <summary>
		/// Regular expression groups are used to identify transitions (e.g. changing from <see langword="True"/> to <see langword="False"/>).
		/// </summary>
		/// <remarks>
		/// When a transition occurs:
		/// 1. the corresponding record is flagged
		/// 2. an appropriate comment is added to the record
		/// Stops within one record when <paramref name="cancellationToken"/> is cancelled, and keeps the flags and comments already changed (#1027).
		/// </remarks>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions">MSDN: Defining RegEx Groups</see>
		public override Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata, CancellationToken cancellationToken)
		{
			if (!TryGetExpressions(records, userDialog, out ImmutableArray<RegularExpression> expressions))
			{
				return new Results(0);
			}

			ClearFlags(records, state, canUpdateMetadata);

			var flaggedLineNumbers = new HashSet<int>();
			var previousState = new Dictionary<string, string>();

			// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
			var buffer = new ContentBuffer();

			for (var position = 0; position < records.Count; position++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var content = records.ReadContent(position, buffer).ToString();

				foreach (KeyValuePair<string, string> currentState in AnalyzerExpressionHelper.GetResolvedKeyValuePairs(expressions, content))
				{
					// A value that appears for the first time, or differs from the key's previous value.
					if (!previousState.TryGetValue(currentState.Key, out var previousValue) || previousValue != currentState.Value)
					{
						var parameterName = RegularExpression.GetFriendlyParameterName(currentState.Key);

						flaggedLineNumbers.Add(records.GetLineNumber(position));

						Flag(records, position, state, $"{parameterName}: {currentState.Value}", canUpdateMetadata);

						previousState[currentState.Key] = currentState.Value;
					}
				}
			}

			return new Results(flaggedLineNumbers.Count);
		}
	}
}
