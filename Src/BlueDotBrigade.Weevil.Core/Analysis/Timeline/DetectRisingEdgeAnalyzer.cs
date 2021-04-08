﻿namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class DetectRisingEdgeAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;

		public DetectRisingEdgeAnalyzer(FilterStrategy filterStrategy)
		{
			_filterStrategy = filterStrategy;
		}

		public string Key => AnalysisType.DetectRisingEdges.ToString();

		public string DisplayName => "Detect Rising Edges";

		/// <summary>
		/// Regular expression groups are used to identify transitions (e.g. changing from <see langword="True"/> to <see langword="False"/>).
		/// </summary>
		/// <remarks>
		/// When a transition occurs:
		/// 1. the corresponding record is flagged
		/// 2. an appropriate comment is added to the record
		/// </remarks>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions">MSDN: Defining RegEx Groups</see>
		public int Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog)
		{
			var flaggedRecords = 0;

			if (_filterStrategy != FilterStrategy.KeepAllRecords)
			{
				if (_filterStrategy.InclusiveFilter.Count > 0)
				{
					var previous = new Dictionary<string, string>();
					List<RegularExpression> expressions = GetRegularExpressions(_filterStrategy.InclusiveFilter.GetExpressions());

					foreach (IRecord record in records)
					{
						record.Metadata.IsFlagged = false;

						foreach (RegularExpression expression in expressions)
						{
							IDictionary<string, string> keyValuePairs = expression.GetKeyValuePairs(record);

							if (keyValuePairs.Count > 0)
							{
								foreach (KeyValuePair<string, string> current in keyValuePairs)
								{
									if (!string.IsNullOrWhiteSpace(current.Value))
									{
										if (previous.ContainsKey(current.Key))
										{
											if (long.TryParse(previous[current.Key], out var previousValue) &&
												long.TryParse(current.Value, out var currentValue))
											{
												if (currentValue > previousValue)
												{
													var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);

													flaggedRecords++;
													record.Metadata.IsFlagged = true;
													record.Metadata.UpdateUserComment($"{parameterName}: {previous[current.Key]} => {current.Value}");
												}
												previous[current.Key] = current.Value;
											}
										}
										else
										{
											var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);

											flaggedRecords++;
											record.Metadata.IsFlagged = true;
											record.Metadata.UpdateUserComment($"{parameterName}: {current.Value}");
											previous.Add(current.Key, current.Value);
										}
									}
								}
							}
						}
					}
				}
			}

			return flaggedRecords;
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