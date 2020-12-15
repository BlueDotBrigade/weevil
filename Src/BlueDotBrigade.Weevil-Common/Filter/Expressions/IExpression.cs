namespace BlueDotBrigade.Weevil.Filter
{
	using Data;

	public interface IExpression
	{
		bool IsMatch(IRecord record);
	}
}
