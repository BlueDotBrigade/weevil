namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using Data;

	public interface IExpression
	{
		bool IsMatch(IRecord record);
	}
}
