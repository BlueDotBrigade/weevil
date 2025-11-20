namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class DetectFallingEdgeAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;

		public DetectFallingEdgeAnalyzer(FilterStrategy filterStrategy)
		{
			_filterStrategy = filterStrategy;
		}

		public string Key => AnalysisType.DetectFallingEdges.ToString();

		public string DisplayName => "Detect Falling Edges";

		/// <summary>
		/// Regular expression groups are used to identify transitions (e.g. changing from <see langword="True"/> to <see langword="False"/>).
		/// </summary>
		/// <remarks>
		/// When a transition occurs:
		/// 1. the corresponding record is flagged
		/// 2. an appropriate comment is added to the record
		/// </remarks>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions">MSDN: Defining RegEx Groups</see>
		public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			var count = 0;

			var analysisOrder = AnalysisHelper.GetAnalysisOrder(userDialog);

			if (AnalysisHelper.CanPerformAnalysis(_filterStrategy))
			{
				var previous = new Dictionary<string, string>();
				ImmutableArray<RegularExpression> expressions = AnalysisHelper.GetRegularExpressions(_filterStrategy);

				var sortedRecords = analysisOrder == AnalysisOrder.Ascending
					? records
					: records.OrderByDescending((x => x.LineNumber)).ToImmutableArray();

				foreach (IRecord record in sortedRecords)
				{
					AnalysisHelper.ClearRecordFlag(record, canUpdateMetadata);

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
												if (currentValue < previousValue)
												{
													var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);

													count++;

													if (canUpdateMetadata)
													AnalysisHelper.UpdateRecordMetadata(
														record,
														true,
														$"{parameterName}: {previous[current.Key]} => {current.Value}",
														canUpdateMetadata);
												previous[current.Key] = current.Value;
											}
										}
										else
										{
											var parameterName = RegularExpression.GetFriendlyParameterName(current.Key);

											count++;

													AnalysisHelper.UpdateRecordMetadata(
														record,
														true,
														$"{parameterName}: {current.Value}",
														canUpdateMetadata);
											
											previous.Add(current.Key, current.Value);
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
