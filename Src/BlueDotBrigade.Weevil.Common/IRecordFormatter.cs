namespace BlueDotBrigade.Weevil
{
	using BlueDotBrigade.Weevil.Data;

	public interface IRecordFormatter
	{
		string Format(IRecord record);
	}
}