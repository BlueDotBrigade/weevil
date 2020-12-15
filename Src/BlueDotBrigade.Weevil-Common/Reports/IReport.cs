namespace BlueDotBrigade.Weevil.Reports
{
	public interface IReport
	{
		void Generate(params object[] userParameters);
	}
}
