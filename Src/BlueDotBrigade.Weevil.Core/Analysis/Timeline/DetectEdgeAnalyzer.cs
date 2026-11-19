namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Globalization;
	using System.Threading;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	/// <summary>
	/// Extracts numeric values via regex named capture groups and compares each with the previous matching value.
	/// Flags the record immediately before the first increase in each run of consecutive increases, or for falling edges,
	/// before the first decrease in each run of consecutive decreases.
	/// Non-matching records are ignored; any other change, or none, ends the current run.
	/// </summary>
	internal sealed class DetectEdgeAnalyzer : RegularExpressionAnalyzer
	{
		private readonly AnalysisType _analysisType;

		/// <param name="analysisType">Either <see cref="AnalysisType.DetectRisingEdges"/> or <see cref="AnalysisType.DetectFallingEdges"/>.</param>
		public DetectEdgeAnalyzer(AnalysisType analysisType, FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander = null)
			: base(filterStrategy, aliasExpander)
		{
			if (analysisType != AnalysisType.DetectRisingEdges && analysisType != AnalysisType.DetectFallingEdges)
			{
				throw new ArgumentOutOfRangeException(nameof(analysisType), analysisType, "Expected rising or falling edges.");
			}

			_analysisType = analysisType;
		}

		public override string Key => _analysisType.ToString();

		public override string DisplayName => _analysisType == AnalysisType.DetectRisingEdges ? "Detect Rising Edges" : "Detect Falling Edges";

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

			AnalysisOrder analysisOrder = AnalysisHelper.GetAnalysisOrder(userDialog);

			ClearFlags(records, state, canUpdateMetadata);

			RecordList sortedRecords = AnalysisHelper.OrderRecordsForAnalysis(records, analysisOrder);

			var flaggedLineNumbers = new HashSet<int>();
			var previous = new Dictionary<string, (string Value, int Position)>();
			var isInRun = new Dictionary<string, bool>();

			// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
			var buffer = new ContentBuffer();

			for (var position = 0; position < sortedRecords.Count; position++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var content = sortedRecords.ReadContent(position, buffer).ToString();

				foreach (KeyValuePair<string, string> current in AnalyzerExpressionHelper.GetResolvedKeyValuePairs(expressions, content))
				{
					if (previous.TryGetValue(current.Key, out (string Value, int Position) prior) &&
						decimal.TryParse(prior.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var previousValue) &&
						decimal.TryParse(current.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var currentValue))
					{
						var isEdge = _analysisType == AnalysisType.DetectRisingEdges
							? currentValue > previousValue
							: currentValue < previousValue;
						var wasInRun = isInRun.TryGetValue(current.Key, out var priorState) && priorState;

						if (isEdge && !wasInRun)
						{
							// Flag the record BEFORE the change: the last stable value (the valley before a rise, or the peak before a fall).
							// This points the user at the last "good" record, with the transition visible
							// in the next record below it — natural for top-down log navigation.
							var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);

							flaggedLineNumbers.Add(sortedRecords.GetLineNumber(prior.Position));

							Flag(sortedRecords, prior.Position, state, $"{parameterName}: {prior.Value} => {current.Value}", canUpdateMetadata);
						}

						isInRun[current.Key] = isEdge;
					}
					else
					{
						isInRun[current.Key] = false;
					}

					previous[current.Key] = (current.Value, position);
				}
			}

			return new Results(flaggedLineNumbers.Count);
		}
	}
}
