namespace BlueDotBrigade.Weevil.Filter.Expressions.Regular
{
	using System.Text.RegularExpressions;

	internal class ExpressionFactory : IExpressionFactory
	{
		private const string IsCaseSensitive = "IsCaseSensitive";

		private readonly bool _isCaseSensitive;

		public ExpressionFactory(IFilterCriteria filterCriteria)
		{
			if (filterCriteria.Configuration.ContainsKey(IsCaseSensitive))
			{
				if (bool.TryParse(filterCriteria.Configuration[IsCaseSensitive].ToString(), out var userConfigurationValue))
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