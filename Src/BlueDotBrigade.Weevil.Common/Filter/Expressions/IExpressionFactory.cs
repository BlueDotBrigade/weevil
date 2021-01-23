namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	public interface IExpressionFactory
	{
		/// <summary>
		/// Attempts to convert the serialized value into an instance of <see cref="IExpression"/>.
		/// </summary>
		bool TryGetExpression(string serializedExpression, out IExpression result);
	}
}
