namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;

	internal class StaticAliasExpander : IStaticAliasExpander
	{
		private static readonly string[] NoExpressions = Array.Empty<string>();

		private readonly IDictionary<string, string> _staticAliases;

		public StaticAliasExpander(IDictionary<string, string> staticAliases)
		{
			_staticAliases = staticAliases ?? throw new ArgumentNullException(nameof(staticAliases));
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
				foreach (KeyValuePair<string, string> alias in _staticAliases)
				{
					// Is this expression an known alias?
					if (expression.IndexOf(alias.Key, StringComparison.InvariantCultureIgnoreCase) >= 0)
					{
						// Case insensitive find & replace
						// https://stackoverflow.com/a/24580455/949681
						expandedFilter = Regex.Replace(expression,
							Regex.Escape(alias.Key),
							Regex.Replace(alias.Value, "\\$[0-9]+", @"$$$0"),
							RegexOptions.IgnoreCase);

						break; // alias found, exit!
					}
				}
			}

			return expandedFilter;
		}
	}
}
