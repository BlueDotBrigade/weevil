namespace BlueDotBrigade.Weevil
{
	using Data;

	public interface IEngine : ICoreEngine
	{
		void Clear(ClearOperation clearOperation);

		void Reload();
	}
}