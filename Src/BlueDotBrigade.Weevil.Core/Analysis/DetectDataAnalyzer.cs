﻿namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class DetectDataAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;

		public DetectDataAnalyzer(FilterStrategy filterStrategy)
		{
			_filterStrategy = filterStrategy;
		}

		public string Key => AnalysisType.DetectData.ToString();

		public string DisplayName => "Detect Data";

		/// <summary>
		/// Extracts key/value pairs defined by regular expression "groups", and then updates the corresponding <see cref="Metadata.Comment"/>.
		/// </summary>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions">MSDN: Defining RegEx Groups</see>
		public int Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var count = 0;

			if (_filterStrategy != FilterStrategy.KeepAllRecords)
			{
				if (_filterStrategy.InclusiveFilter.Count > 0)
				{
					List<RegularExpression> expressions = GetRegularExpressions(_filterStrategy.InclusiveFilter.GetExpressions());

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
								foreach (KeyValuePair<string, string> keyValuePair in keyValuePairs)
								{
									if (!string.IsNullOrWhiteSpace(keyValuePair.Value))
									{
										var parameterName = RegularExpression.GetFriendlyParameterName(keyValuePair.Key);

										if (canUpdateMetadata)
										{
											record.Metadata.IsFlagged = true;
											record.Metadata.UpdateUserComment($"{parameterName}: {keyValuePair.Value}");
										}

										count++;
									}
								}
							}
						}
					}
				}
			}

			return count;
		}

		private static List<RegularExpression> GetRegularExpressions(ImmutableArray<IExpression> expressions)
		{
			var results = new List<RegularExpression>();

			foreach (IExpression expression in expressions)
			{
				if (expression is RegularExpression)
				{
					results.Add(expression as RegularExpression);
				}
			}

			return results;
		}
	}
}
