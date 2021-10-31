namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;

	internal class FilterAliasExpander : IFilterAliasExpander
	{
		private static readonly string[] NoExpressions = Array.Empty<string>();

		private readonly IDictionary<string, string> _filterAliases;

		public FilterAliasExpander(IDictionary<string, string> filterAliases)
		{
			_filterAliases = filterAliases ?? throw new ArgumentNullException(nameof(filterAliases));
		}
		/// <summary>
		/// Replaces known aliases with their fully qualified representation.
		/// </summary>
		/// <param name="expressions">Represents a list of expressions that may, or may not, reference a known alias.</param>
		public string[] Expand(string[] expressions)
		{
			var expandedExpressions = NoExpressions;

			if (expressions != null)
			{
				expandedExpressions = new string[expressions.Length];

				for (var i = 0; i < expressions.Length; i++)
				{
					expandedExpressions[i] = ExpandExpression(expressions[i]);
				}

			}

			return expandedExpressions;
		}

		public string Expand(string filter)
		{
			var result = string.Empty;

			if (!string.IsNullOrWhiteSpace(filter))
			{
				var delimiters = new string[]
				{
					Constants.FilterOrOperator,
				};

				var expressions = filter.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
				var expandedExpressions = Expand(expressions);

				result = string.Join(Constants.FilterOrOperator, expandedExpressions);
			}

			return result;
		}

		private string ExpandExpression(string expression)
		{
			var expandedFilter = expression;

			if (!string.IsNullOrEmpty(expression))
			{
				foreach (KeyValuePair<string, string> alias in _filterAliases)
				{
					if (string.Equals(expression.Trim(), alias.Key.Trim(), StringComparison.InvariantCultureIgnoreCase))
					{
						expandedFilter = alias.Value;
					}
				}
			}

			return expandedFilter;
		}
	}
}
