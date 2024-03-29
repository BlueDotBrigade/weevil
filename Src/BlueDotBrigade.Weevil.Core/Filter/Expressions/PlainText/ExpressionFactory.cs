﻿namespace BlueDotBrigade.Weevil.Filter.Expressions.PlainText
{
	using BlueDotBrigade.Weevil.Filter.Expressions;

	internal class ExpressionFactory : IExpressionFactory
	{
		public ExpressionFactory(IFilterCriteria criteria)
		{
			// nothing to do
		}

		public bool TryGetExpression(string serializedExpression, out IExpression result)
		{
			result = new PlainTextExpression(serializedExpression, true);
			return SurrogateExpression.IsReal(result);
		}
	}
}
