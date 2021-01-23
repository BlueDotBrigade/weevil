namespace BlueDotBrigade.Weevil.Data
{
	public interface IRecordParser
	{
		bool TryParse(int line, string content, out Record record);
	}
}