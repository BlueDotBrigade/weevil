namespace BlueDotBrigade.Weevil
{
	public delegate IEngineBuilder CreateEngineBuilder(string source);

	public interface IEngineBuilder
	{
		IEngineBuilder UsingContext(ContextDictionary context);
		IEngineBuilder UsingLimit(int maxRecords);

		IEngineBuilder UsingRange(Range range);

		IEngine Open();
	}
}
