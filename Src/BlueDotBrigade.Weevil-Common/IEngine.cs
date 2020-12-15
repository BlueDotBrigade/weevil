namespace BlueDotBrigade.Weevil
{
	using Data;

	public interface IEngine : ICoreEngine
	{
		void Clear(ClearRecordsOperation clearOperation);

		void Reload();
	}
}