namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	public class EngineBuilder : IEngineBuilder
	{
		private readonly CoreEngine.CoreEngineBuilder _coreEngineBuilder = null;

		public EngineBuilder(string sourceFilePath)
		{
			_coreEngineBuilder = new CoreEngine.CoreEngineBuilder(sourceFilePath);
		}

		public EngineBuilder(string sourceFilePath, int startAtLineNumber)
		{
			_coreEngineBuilder = new CoreEngine.CoreEngineBuilder(sourceFilePath, startAtLineNumber);
		}

		public IEngineBuilder UsingContext(ContextDictionary context)
		{
			_coreEngineBuilder.UsingContext(context);
			return this;
		}

		public IEngineBuilder UsingTelemetry(ITelemetryMetricRecorder recorder)
		{
			_coreEngineBuilder.UsingTelemetry(recorder);
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

		/// <summary>
		/// Opens the log in RAM or on disk (#1025). Internal until the Open As window lets the user choose (#1028).
		/// </summary>
		internal IEngineBuilder UsingStorage(StorageMode storageMode)
		{
			_coreEngineBuilder.UsingStorage(storageMode);
			return this;
		}

		public IEngine Open()
		{
			return new Engine(_coreEngineBuilder.Build());
		}
	}
}
