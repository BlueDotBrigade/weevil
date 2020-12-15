namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Data;
	using Filter;
	using Filter.Expressions.Regular;

	internal class DetectDataAnalyzer : IRecordCollectionAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;
		private readonly ImmutableArray<IRecord> _records;

		public DetectDataAnalyzer(FilterStrategy filterStrategy, ImmutableArray<IRecord> records)
		{
			_filterStrategy = filterStrategy;
			_records = records;
		}

		/// <summary>
		/// Extracts key/value pairs defined by regular expression "groups", and then updates the corresponding <see cref="Metadata.Comment"/>.
		/// </summary>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions">MSDN: Defining RegEx Groups</see>
		public IDictionary<string, object> Analyze(params object[] userParameters)
		{
			var count = 0;

			if (_filterStrategy != FilterStrategy.KeepAllRecords)
			{
				if (_filterStrategy.InclusiveFilter.Count > 0)
				{
					List<RegularExpression> expressions = GetRegularExpressions(_filterStrategy.InclusiveFilter.GetExpressions());

					foreach (IRecord record in _records)
					{
						record.Metadata.IsFlagged = false;

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
										record.Metadata.IsFlagged = true;
										record.Metadata.UpdateUserComment($"{parameterName}: {keyValuePair.Value}");

										count++;
									}
								}
							}
						}
					}
				}
			}

			return new Dictionary<string, object>
			{
				{ "KeysFound", count },
			};
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
