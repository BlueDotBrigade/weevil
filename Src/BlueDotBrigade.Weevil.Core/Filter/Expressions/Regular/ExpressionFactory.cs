namespace BlueDotBrigade.Weevil.Filter.Expressions.Regular
{
	using System.Text.RegularExpressions;
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
			result = SurrogateExpression.Instance;

			if (!string.IsNullOrEmpty(serializedExpression))
			{
				var regexOptions = RegexOptions.Compiled;

				if (_isCaseSensitive)
				{
					// use RegEx default: case sensitive search
				}
				else
				{
					regexOptions |= RegexOptions.IgnoreCase;
				}
				result = new RegularExpression(serializedExpression, regexOptions);
			}

			return SurrogateExpression.IsReal(result);
		}
	}
}