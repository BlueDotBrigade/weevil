namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	internal class ExpressionComposite : IExpression
	{
		private readonly ImmutableArray<IExpression> _expressions;

		public ExpressionComposite(ImmutableArray<IExpression> expressions)
		{
			_expressions = expressions;
		}

		public int Count => _expressions.Length;

		public bool IsMatch(IRecord record)
		{
			bool isMatch = false;

			foreach (var expression in _expressions)
			{
				if (expression.IsMatch(record))
				{
					isMatch = true;
					break;
				}
			}
			return isMatch;
		}

        public ImmutableArray<IExpression> GetExpressions()
        {
            return _expressions;
        }
	}
}
