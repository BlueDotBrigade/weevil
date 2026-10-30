namespace BlueDotBrigade.Weevil.Filter.Expressions.PlainText
{
	using System;
	using System.Globalization;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Data;

	internal class PlainTextExpression : IExpression
	{
		public static readonly char[] Delimiters = new char[] { '|' };

		private readonly bool _isCaseSensitive;
		private readonly string _expression;

		public PlainTextExpression(string expression, bool isCaseSensitive)
		{
			_expression = expression;
			_isCaseSensitive = isCaseSensitive;
		}

		public bool IsMatch(IRecord record, ContentBuffer content)
		{
			ReadOnlySpan<char> text = content.Read(record);

			if (_isCaseSensitive)
			{
				return text.Contains(_expression, StringComparison.Ordinal);
			}
			else
			{
				return CultureInfo.InvariantCulture.CompareInfo.IndexOf(text, _expression, CompareOptions.IgnoreCase) >= 0;
			}
		}
	}
}