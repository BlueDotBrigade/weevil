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
			var results = GetKeyValuePairs(record.Content);

			if (results.Count > 0)
			{
				Log.Default.Write(
					LogSeverityType.Trace,
					$"Data found within record content. {nameof(record.LineNumber)}={record.LineNumber}, TotalMatches={results.Count}");
			}

			return results;
		}

		public IDictionary<string, string> GetKeyValuePairs(string value)
		{
				var results = new Dictionary<string, string>();

			MatchCollection matches = _expression.Matches(value);
			
			var groupNames = _expression.GetGroupNames();

			foreach (Match match in matches)
			{
				foreach (var groupName in groupNames)
				{
					var groupValue = match.Groups[groupName].Value;
					if (int.TryParse(groupName, out var groupNumber))
					{
						// For now, we only care about named groups.
						// ... Ignore numbered groups.
					}
					else
					{
						if (results.ContainsKey(groupName))
						{
							throw new InvalidExpressionException(
								_expressionValue,
								$"A named group should only return one matching value. Key={groupName}"
							);

						}
						else
						{
							results.Add(groupName, groupValue);
						}
					}
				}
			}

			return results;
		}

		public static string GetFriendlyParameterName(string key)
		{
			return key;
		}
	}
}