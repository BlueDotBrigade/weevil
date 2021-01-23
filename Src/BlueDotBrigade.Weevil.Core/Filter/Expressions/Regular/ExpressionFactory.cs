namespace BlueDotBrigade.Weevil.Filter.Expressions.Regular
{
	using System.Text.RegularExpressions;

	internal class ExpressionFactory : IExpressionFactory
	{
		public ExpressionFactory(IFilterCriteria criteria)
		{
			// nothing to do
		}

		public bool TryGetExpression(string serializedExpression, out IExpression result)
		{
			result = SurrogateExpression.Instance;

			if (!string.IsNullOrEmpty(serializedExpression))
			{
				result = new RegularExpression(serializedExpression, RegexOptions.Compiled);
			}

			return SurrogateExpression.IsReal(result);
		}
	}
}