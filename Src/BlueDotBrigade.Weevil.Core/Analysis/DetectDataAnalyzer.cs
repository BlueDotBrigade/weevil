namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Threading;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	/// <summary>
	/// Extracts key-value pairs from every record using regex named capture groups,
	/// flagging each matching record and annotating it with the extracted values.
	/// </summary>
	internal class DetectDataAnalyzer : RegularExpressionAnalyzer
	{
		public DetectDataAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander = null)
			: base(filterStrategy, aliasExpander)
		{
		}

		public override string Key => AnalysisType.DetectData.ToString();

		public override string DisplayName => "Detect Data";

		/// <summary>
		/// Extracts key/value pairs defined by regular expression "groups", and then adds them to the records' comments.
		/// </summary>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions">MSDN: Defining RegEx Groups</see>
		public override Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata, CancellationToken cancellationToken)
		{
			if (!TryGetExpressions(records, userDialog, out ImmutableArray<RegularExpression> expressions))
			{
				return new Results(0);
			}

			ClearFlags(records, state, canUpdateMetadata);

			var flaggedLineNumbers = new HashSet<int>();

			// One buffer, so each chunk of a log on disk is read once rather than once per record (#1027).
			var buffer = new ContentBuffer();

			for (var position = 0; position < records.Count; position++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				// Track unique key/value pairs per record to avoid duplicates using tuple-based key
				var seenKeyValues = new HashSet<(string Key, string Value)>();

				// Read once, however many expressions test the record (#1133).
				var content = records.ReadContent(position, buffer).ToString();

				foreach (RegularExpression regexExpression in expressions)
				{
					foreach (KeyValuePair<string, string> keyValuePair in regexExpression.GetKeyValuePairs(content))
					{
						// Skip blank values, and duplicate key/value pairs within the same record
						if (!string.IsNullOrWhiteSpace(keyValuePair.Value) && seenKeyValues.Add((keyValuePair.Key, keyValuePair.Value)))
						{
							var parameterName = RegularExpression.GetFriendlyParameterName(keyValuePair.Key);

							Flag(records, position, state, $"{parameterName}: {keyValuePair.Value}", canUpdateMetadata);

							flaggedLineNumbers.Add(records.GetLineNumber(position));
						}
					}
				}
			}

			return new Results(flaggedLineNumbers.Count);
		}
	}
}
