namespace BlueDotBrigade.Weevil.Filter.Expressions.Regular
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using Data;
	using Diagnostics;

	[DebuggerDisplay("Expression={_expressionValue}")]
	public class RegularExpression : IExpression
	{
		private const string Delimiter = "__";

		private readonly Regex _expression;
		private readonly string _expressionValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="RegularExpression"/> class for the specified regular expression.
		/// </summary>
		/// <param name="expression">The regular expression pattern to match.</param>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference">MSDN: RegEx Quick Reference</see>
		/// <see href="https://download.microsoft.com/download/D/2/4/D240EBF6-A9BA-4E4F-A63F-AEB6DA0B921C/Regular%20expressions%20quick%20reference.pdf">MSDN: RegEx Quick Reference (PDF)</see>
		public RegularExpression(string expression)
			 : this(expression, RegexOptions.None)
		{
			// nothing to do
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RegularExpression"/> class for the specified regular expression.
		/// </summary>
		/// <param name="expression">The regular expression pattern to match.</param>
		/// <param name="options">A bitwise combination of the enumeration values that modify the regular expression.</param>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference">MSDN: RegEx Quick Reference</see>
		/// <see href="https://download.microsoft.com/download/D/2/4/D240EBF6-A9BA-4E4F-A63F-AEB6DA0B921C/Regular%20expressions%20quick%20reference.pdf">MSDN: RegEx Quick Reference (PDF)</see>
		public RegularExpression(string expression, RegexOptions options)
		{
			try
			{
				_expressionValue = expression;
				_expression = new Regex(expression, RegexOptions.Compiled | options);
			}
			catch (Exception e)
			{
				Log.Default.Write(
					 LogSeverityType.Error,
					 "The regular expression is not valid.",
					 new Dictionary<string, object>
					 {
								{ "Expression", expression},
					 });

				throw new InvalidExpressionException(expression, e);
			}
		}

		public bool IsMatch(IRecord record)
		{
			return _expression.IsMatch(record.Content);
		}

		public IDictionary<string, string> GetKeyValuePairs(IRecord record)
		{
			var results = new Dictionary<string, string>();

			MatchCollection matches = _expression.Matches(record.Content);

			var unnamedGroups = 0;
			var namedGroups = 0;

			foreach (Match match in matches)
			{
				for (var i = 1; i < match.Groups.Count; i++)
				{
					if (int.TryParse(match.Groups[i].Name, out var groupNumber))
					{
						unnamedGroups++;
						// For now, we only care about named groups.
						// ... Ignore numbered groups.
					}
					else
					{
						namedGroups++;

						var key = $"{match.Groups[i].Name}{Delimiter}{match.Name}";

						if (results.ContainsKey(key))
						{
							throw new InvalidExpressionException(
								_expressionValue,
								$"A named group should only return one matching value. Key={match.Groups[i].Name}"
							);

						}
						else
						{
							results.Add(key, match.Groups[i].Value);
						}
					}
				}
			}

			if (unnamedGroups + namedGroups > 0)
			{
				Log.Default.Write(
					LogSeverityType.Trace,
					$"Data found within record content. {nameof(record.LineNumber)}={record.LineNumber}, UnnamedVariables={unnamedGroups}, NamedVariables={namedGroups}, TotalVariables={unnamedGroups + namedGroups}");
			}

			return results;
		}

		public static string GetFriendlyParameterName(string key)
		{
			var result = string.Empty;

			if (!string.IsNullOrWhiteSpace(key))
			{
				result = key.Substring(0, key.IndexOf(Delimiter, StringComparison.InvariantCultureIgnoreCase));
			}

			return result;
		}
	}
}