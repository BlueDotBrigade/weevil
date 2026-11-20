namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	public class EngineBuilder : IEngineBuilder
	{
		private readonly CoreEngine.CoreEngineBuilder _coreEngineBuilder = null;

		private readonly string _sourceFilePath;

		private Engine _replaced;

		public EngineBuilder(string sourceFilePath)
		{
			_sourceFilePath = sourceFilePath;
			_coreEngineBuilder = new CoreEngine.CoreEngineBuilder(sourceFilePath);
		}

		public EngineBuilder(string sourceFilePath, int startAtLineNumber)
		{
			_sourceFilePath = sourceFilePath;
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

		/// <inheritdoc />
		public IEngineBuilder UsingRegion(string regionName)
		{
			_coreEngineBuilder.UsingRegion(regionName);
			return this;
		}

		/// <inheritdoc />
		public IEngineBuilder UsingSection(string sectionName)
		{
			_coreEngineBuilder.UsingSection(sectionName);
			return this;
		}

		/// <inheritdoc />
		public IEngineBuilder UsingStorage(StorageMode storageMode)
		{
			_coreEngineBuilder.UsingStorage(storageMode);
			return this;
		}

		/// <inheritdoc />
		public IEngineBuilder UsingDisplayPath(string displayPath)
		{
			_coreEngineBuilder.UsingDisplayPath(displayPath);
			return this;
		}

		/// <inheritdoc />
		public IEngineBuilder Replacing(IEngine engine)
		{
			// Only an engine of this assembly holds a lock.
			_replaced = engine as Engine;
			return this;
		}

		/// <inheritdoc />
		public IEngine Open()
		{
			// Taken before the log is read, so a log another Weevil has open is refused before anything reads it or writes its saved index (#1254).
			// The engine replaced may already hold it, as when the GUI opens again the log it has open.
			LogLock logLock = _replaced?.IsLocking(_sourceFilePath) == true
				? null
				: LogLock.Acquire(_sourceFilePath);

			CoreEngine coreEngine;

			try
			{
				coreEngine = _coreEngineBuilder.Build();
			}
			catch
			{
				logLock?.Dispose();
				throw;
			}

			return new Engine(coreEngine, logLock ?? _replaced?.TakeLock(_sourceFilePath));
		}
	}
}
