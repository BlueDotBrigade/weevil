namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using Data;

	public class LogicalOrOperation
	{
		private readonly ImmutableArray<IExpression> _expressions;

		public LogicalOrOperation(ImmutableArray<IExpression> expressions)
		{
			_expressions = expressions;
		}

		public int Count => _expressions.Length;

		/// <summary>
		/// This operator returns <see langword="True"/> if any of the <see cref="IExpression"/> values match the given <see cref="IRecord"/>.
		/// </summary>
		public bool IsMatch(IRecord record)
		{
			var isMatch = false;

			foreach (IExpression expression in _expressions)
			{
				if (expression.IsMatch(record))
				{
					isMatch = true;
					break;
				}
			}
			return isMatch;
		}

		public ImmutableArray<IExpression> GetAllExpressions()
		{
			return _expressions;
		}

		public ImmutableArray<RegularExpression> GetRegularExpressions()
		{
			var results = new List<RegularExpression>();

			foreach (IExpression expression in _expressions)
			{
				if (expression is RegularExpression)
				{
					results.Add(expression as RegularExpression);
				}
			}

			return results.ToImmutableArray();
		}
	}
}
