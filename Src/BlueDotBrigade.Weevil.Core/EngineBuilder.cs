namespace BlueDotBrigade.Weevil
{
	using System;

	public class EngineBuilder : IEngineBuilder
	{
		private readonly CoreEngine.CoreEngineBuilder _coreEngineBuilder = null;

		public EngineBuilder(string sourceFilePath)
		{
			_coreEngineBuilder = new CoreEngine.CoreEngineBuilder(sourceFilePath);
		}

		public EngineBuilder(string sourceFilePath, int lineNumber)
		{
			_coreEngineBuilder = new CoreEngine.CoreEngineBuilder(sourceFilePath, lineNumber);
		}

		public IEngineBuilder UsingContext(ContextDictionary context)
		{
			_coreEngineBuilder.UsingContext(context);
			return this;
		}

		public IEngineBuilder UsingLimit(int maxRecords)
		{
			_coreEngineBuilder.UsingLimit(maxRecords);
			return this;
		}

		public IEngineBuilder UsingRange(Range range)
		{
			_coreEngineBuilder.UsingRange(range);
			return this;
		}

		public IEngine Open()
		{
			return new Engine(_coreEngineBuilder.Build());
		}
	}
}
