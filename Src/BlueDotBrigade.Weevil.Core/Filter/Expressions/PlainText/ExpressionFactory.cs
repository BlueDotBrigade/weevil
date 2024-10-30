namespace BlueDotBrigade.Weevil.Filter.Expressions.PlainText
{
	using BlueDotBrigade.Weevil.Filter.Expressions;

	internal class ExpressionFactory : IExpressionFactory
	{
		public const string IsCaseSensitiveParameter = "IsCaseSensitive";

		private readonly bool _isCaseSensitive;

		public ExpressionFactory(IFilterCriteria filterCriteria)
		{
			// When no configuration is provided 
			// ... Default: case sensitive filtering
			_isCaseSensitive = true;

			if (filterCriteria.Configuration.ContainsKey(IsCaseSensitiveParameter))
			{
				if (bool.TryParse(filterCriteria.Configuration[IsCaseSensitiveParameter].ToString(), out var userConfigurationValue))
				{
					_isCaseSensitive = userConfigurationValue;
				}
			}
		}

		public bool TryGetExpression(string serializedExpression, out IExpression result)
		{
			// This fixes a bug whereby "plain text" was ALWAYS case sensitive
			result = new PlainTextExpression(serializedExpression, _isCaseSensitive);
			return SurrogateExpression.IsReal(result);
		}
	}
}