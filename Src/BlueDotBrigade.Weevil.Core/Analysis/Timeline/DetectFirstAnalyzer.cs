namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class DetectFirstAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;

		public DetectFirstAnalyzer(FilterStrategy filterStrategy)
		{
			_filterStrategy = filterStrategy;
		}

		public string Key => AnalysisType.DetectFirst.ToString();

		public string DisplayName => "Detect First";

		public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var count = 0;

			if (_filterStrategy != FilterStrategy.KeepAllRecords)
			{
				if (_filterStrategy.InclusiveFilter.Count > 0)
				{
					var foundValues = new HashSet<string>();
					ImmutableArray<RegularExpression> expressions = _filterStrategy.InclusiveFilter.GetRegularExpressions();

					foreach (IRecord record in records)
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
								foreach (KeyValuePair<string, string> pair in keyValuePairs)
								{
									if (!string.IsNullOrWhiteSpace(pair.Value))
									{
										var uniqueKey = $"{pair.Key}:{pair.Value}";
										if (!foundValues.Contains(uniqueKey))
										{
											var parameterName = RegularExpression.GetFriendlyParameterName(pair.Key);

											if (canUpdateMetadata)
											{
												record.Metadata.IsFlagged = true;
												record.Metadata.UpdateUserComment($"{parameterName}: {pair.Value}");
											}

											foundValues.Add(uniqueKey);
											count++;
										}
									}
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
