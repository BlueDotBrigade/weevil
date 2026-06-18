namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Diagnostics;

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

		IEngine Open();
	}
}
