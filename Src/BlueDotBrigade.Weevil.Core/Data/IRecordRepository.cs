namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Immutable;

	public interface IRecordRepository
	{
		IRecord GetNext();

		ImmutableArray<IRecord> GetAll();

		ImmutableArray<IRecord> Get(int maximumCount);
	}
}