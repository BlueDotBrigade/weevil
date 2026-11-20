namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Threading;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	/// <summary>
	/// Parses regex named capture groups and flags only the first, or only the last, record where each unique key/value appears.
	/// Other records with the same extracted value are ignored.
	/// </summary>
	internal sealed class OccurrenceAnalyzer : RegularExpressionAnalyzer
	{
		private readonly AnalysisType _analysisType;

		/// <param name="analysisType">
		/// <see cref="AnalysisType.FirstOccurrence"/> reads the records from the first, and <see cref="AnalysisType.LastOccurrence"/> from the last.
		/// </param>
		public OccurrenceAnalyzer(AnalysisType analysisType, FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander = null)
			: base(filterStrategy, aliasExpander)
		{
			if (analysisType != AnalysisType.FirstOccurrence && analysisType != AnalysisType.LastOccurrence)
			{
				throw new ArgumentOutOfRangeException(nameof(analysisType), analysisType, "Expected the first or the last occurrence.");
			}

			_analysisType = analysisType;
		}

		public override string Key => _analysisType.ToString();

		public override string DisplayName => _analysisType == AnalysisType.FirstOccurrence ? "First Occurrence" : "Last Occurrence";

		public override Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata, CancellationToken cancellationToken)
		{
			if (!TryGetExpressions(records, userDialog, out ImmutableArray<RegularExpression> expressions))
			{
				return new Results(0);
			}

			ClearFlags(records, state, canUpdateMetadata);

			var flaggedLineNumbers = new HashSet<int>();
			var foundValues = new HashSet<string>();

			// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
			var buffer = new ContentBuffer();

			var count = records.Count;

			for (var index = 0; index < count; index++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var position = _analysisType == AnalysisType.FirstOccurrence ? index : count - 1 - index;

				// Read once, however many expressions test the record (#1133).
				var content = records.ReadContent(position, buffer).ToString();

				foreach (RegularExpression expression in expressions)
				{
					foreach (KeyValuePair<string, string> pair in expression.GetKeyValuePairs(content))
					{
						if (!string.IsNullOrWhiteSpace(pair.Value) && foundValues.Add($"{pair.Key}:{pair.Value}"))
						{
							var parameterName = RegularExpression.GetFriendlyParameterName(pair.Key);

							Flag(records, position, state, $"{parameterName}: {pair.Value}", canUpdateMetadata);

							flaggedLineNumbers.Add(records.GetLineNumber(position));
						}
					}
				}
			}

			return new Results(flaggedLineNumbers.Count);
		}
	}
}
