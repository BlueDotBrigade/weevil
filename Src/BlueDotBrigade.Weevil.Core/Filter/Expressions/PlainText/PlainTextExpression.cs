namespace BlueDotBrigade.Weevil.Filter.Expressions.PlainText
{
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

		public bool IsMatch(IRecord record)
		{
			if (_isCaseSensitive)
			{
				return record.Content.Contains(_expression);
			}
			else
			{
				return CultureInfo.InvariantCulture.CompareInfo.IndexOf(record.Content, _expression, CompareOptions.IgnoreCase) >= 0;
			}
		}
	}
}