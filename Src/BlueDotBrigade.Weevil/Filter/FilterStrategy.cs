namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Data;
	using Diagnostics;
	using Expressions;
	using Expressions.Monikers;

	internal class FilterStrategy : IFilterStrategy
	{
		private static readonly FilterStrategy _instance = new FilterStrategy();

		private readonly FilterType _filterType;
		private readonly IFilterCriteria _filterCriteria;

		public static FilterStrategy KeepAllRecords => _instance;

		public static readonly string[] ExpressionDelimiter = new string[] { "||" };

		private const string HideDebugRecords = "HideDebugRecords";
		private const string HideTraceRecords = "HideTraceRecords";
		private const string IncludePinned = "IncludePinned";

		private readonly LogicalOrOperation _inclusiveFilter;
		private readonly LogicalOrOperation _exclusiveFilter;

		private readonly bool _includePinned;

		static FilterStrategy()
		{
			// static constructor required
			// ... to tell C# compiler not to mark type as beforefieldinit
			// ... https://csharpindepth.com/articles/singleton
		}

		private FilterStrategy()
		{
			// nothing to do
		}


		public FilterStrategy(
			ICoreExtension coreExtension,
			ContextDictionary context,
			IStaticAliasExpander staticAliasExpander,
			FilterType filterType,
			IFilterCriteria filterCriteria)
		{
			_filterType = filterType;
			_filterCriteria = filterCriteria;

			var expressionFactory = ExpressionBuilder.Create(coreExtension, context, filterType, filterCriteria);

			List<IExpression> inclusiveExpressions = ConvertCriteriaIntoExpressions(staticAliasExpander, expressionFactory, filterCriteria, true);
			List<IExpression> exclusiveExpressions = ConvertCriteriaIntoExpressions(staticAliasExpander, expressionFactory, filterCriteria, false);

			exclusiveExpressions.AddRange(ConvertConfigurationIntoExpressions(filterCriteria.Configuration));

			_inclusiveFilter = new LogicalOrOperation(ImmutableArray.Create(inclusiveExpressions.ToArray()));
			_exclusiveFilter = new LogicalOrOperation(ImmutableArray.Create(exclusiveExpressions.ToArray()));

			if (filterCriteria.Configuration.ContainsKey(IncludePinned))
			{
				if (bool.TryParse(filterCriteria.Configuration[IncludePinned].ToString(), out var userConfigurationValue))
				{
					_includePinned = userConfigurationValue;
				}
			}
		}

		public FilterType FilterType => _filterType;

		public IFilterCriteria FilterCriteria => _filterCriteria;

		public LogicalOrOperation InclusiveFilter => _inclusiveFilter;

		public LogicalOrOperation ExclusiveFilter => _exclusiveFilter;

		public bool CanKeep(IRecord record)
		{
			var canKeepRecord = false;

			if (_inclusiveFilter.Count == 0 && _exclusiveFilter.Count == 0)
			{
				canKeepRecord = true;
			}
			else
			{
				if (_includePinned && record.Metadata.IsPinned)
				{
					canKeepRecord = true;
				}
				else
				{
					if (_inclusiveFilter.Count > 0 && _exclusiveFilter.Count == 0)
					{
						canKeepRecord = _inclusiveFilter.ReturnsTrue(record);
					}
					else if (_inclusiveFilter.Count == 0 && _exclusiveFilter.Count > 0)
					{
						canKeepRecord = !_exclusiveFilter.ReturnsTrue(record);
					}
					else
					{
						if (_inclusiveFilter.ReturnsTrue(record))
						{
							var isRecordIgnored = _exclusiveFilter.ReturnsTrue(record);
							canKeepRecord = !isRecordIgnored;
						}
					}
				}
			}

			return canKeepRecord;
		}

		private static List<IExpression> ConvertCriteriaIntoExpressions(IStaticAliasExpander staticAliasExpander, ExpressionBuilder expressionBuilder, IFilterCriteria filterCriteria, bool forInclusiveFilter)
		{
			var results = new List<IExpression>();

			var filter = forInclusiveFilter ? filterCriteria.Include : filterCriteria.Exclude;

			filter = staticAliasExpander.Expand(filter);

			var expressions = filter.Split(ExpressionDelimiter, StringSplitOptions.RemoveEmptyEntries);

			foreach (var serializedValue in expressions)
			{
				if (expressionBuilder.TryGetExpression(serializedValue, out IExpression expression))
				{
					results.Add(expression);
				}
			}

			return results;
		}

		private static List<IExpression> ConvertConfigurationIntoExpressions(IReadOnlyDictionary<string, object> configuration)
		{
			var results = new List<IExpression>();

			if (configuration.ContainsKey(HideDebugRecords))
			{
				if (bool.TryParse(configuration[HideDebugRecords].ToString(), out var hideRecords))
				{
					if (hideRecords)
					{
						results.Add(new SeverityTypeExpression(SeverityType.Debug));
						Log.Default.Write(LogSeverityType.Debug, $"Records of type `Debug` will not be included in the results.");
					}
				}
			}

			if (configuration.ContainsKey(HideTraceRecords))
			{
				if (bool.TryParse(configuration[HideTraceRecords].ToString(), out var hideRecords))
				{
					if (hideRecords)
					{
						results.Add(new SeverityTypeExpression(SeverityType.Verbose));
						Log.Default.Write(LogSeverityType.Debug, $"Records of type `Trace` will not be included in the results.");
					}
				}
			}

			return results;
		}
	}
}
