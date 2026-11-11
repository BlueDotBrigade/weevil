namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	public delegate IEngineBuilder CreateEngineBuilder(string source);

	public interface IEngineBuilder
	{
		IEngineBuilder UsingContext(ContextDictionary context);

		/// <summary>
		/// Records semantic usage metrics for operations performed by the engine (for example, applying a filter).
		/// </summary>
		/// <param name="recorder">The recorder that usage metrics are sent to.</param>
		IEngineBuilder UsingTelemetry(ITelemetryMetricRecorder recorder);

		/// <summary>
		/// During the loading process the total number of records will be limited to the specified value.
		/// </summary>
		/// <param name="maxRecords">Represents the maximum number of records that can be returned.</param>
		IEngineBuilder UsingLimit(int maxRecords);

		/// <summary>
		/// During the loading process the records whose line number are within range will be loaded.
		/// </summary>
		/// <param name="range">Represents the line numbers that will be loaded.</param>
		IEngineBuilder UsingRange(Range range);

		/// <summary>
		/// Keeps the log's content in RAM, the default, or reads it back from the log whenever it is needed (#421).
		/// </summary>
		/// <remarks>
		/// <see cref="IEngine.Reload"/> and <see cref="IEngine.Clear"/> keep the mode.
		/// On disk, a read of a record's content can throw an <see cref="System.IO.IOException"/>, such as <see cref="LogFileChangedException"/>, once the log changed or is gone.
		/// </remarks>
		/// <param name="storageMode">Where the log's content is kept.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="storageMode"/> is not a <see cref="StorageMode"/>.</exception>
		IEngineBuilder UsingStorage(StorageMode storageMode);

		IEngine Open();
	}
}
