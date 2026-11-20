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
		/// During the loading process only the records of the region with this name will be loaded, from the regions the log's sidecar lists (#1213).
		/// </summary>
		/// <remarks>
		/// The region replaces any range. <see cref="Open"/> throws a <see cref="System.Collections.Generic.KeyNotFoundException"/>
		/// when the sidecar lists no region with this name, and an <see cref="InvalidOperationException"/> when a section was also named.
		/// </remarks>
		/// <param name="regionName">The region's name, ignoring case.</param>
		/// <exception cref="ArgumentException"><paramref name="regionName"/> is empty.</exception>
		IEngineBuilder UsingRegion(string regionName);

		/// <summary>
		/// During the loading process only the records of the level-1 section with this name will be loaded, from the log's sidecar (#1212):
		/// from its line to the line before the next level-1 section, or to the end of the log, so the sections nested in it are loaded too.
		/// </summary>
		/// <remarks>
		/// The section replaces any range. <see cref="Open"/> throws a <see cref="System.Collections.Generic.KeyNotFoundException"/>
		/// when the sidecar lists no level-1 section with this name, and an <see cref="InvalidOperationException"/> when a region was also named.
		/// </remarks>
		/// <param name="sectionName">The section's name, ignoring case.</param>
		/// <exception cref="ArgumentException"><paramref name="sectionName"/> is empty.</exception>
		IEngineBuilder UsingSection(string sectionName);

		/// <summary>
		/// Keeps the log's content in RAM, the default, or reads it back from the log whenever it is needed (#421).
		/// </summary>
		/// <remarks>
		/// <see cref="IEngine.Reload"/> and <see cref="IEngine.Clear"/> keep the mode.
		/// On disk, a read of a record's content throws <see cref="LogFileChangedException"/> once the log changed or is gone (#1295).
		/// </remarks>
		/// <param name="storageMode">Where the log's content is kept.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="storageMode"/> is not a <see cref="StorageMode"/>.</exception>
		IEngineBuilder UsingStorage(StorageMode storageMode);

		/// <summary>
		/// Names the log as the analyst knows it, when the path given to the builder is a copy, such as one extracted from a zip (#1312).
		/// </summary>
		/// <remarks>
		/// <see cref="ICoreEngine.SourceFilePath"/>, and messages about the log such as <see cref="LogFileChangedException.Path"/>, give this path, while the copy is read.
		/// A zip reads like a folder, as Explorer shows it: for example <c>C:\Logs\Archive.zip\App.log</c>. Reload keeps it.
		/// Without it, both are the path given to the builder.
		/// </remarks>
		IEngineBuilder UsingDisplayPath(string displayPath);

		/// <summary>
		/// Names the engine this one replaces, as when an analyst opens a log in the window that shows another (#1254).
		/// When both read the same file, the new engine takes over its lock once it has opened, so Weevil can open again the log it has open.
		/// </summary>
		/// <remarks>
		/// If the open fails, the lock stays with <paramref name="engine"/>. Either way, the caller still disposes <paramref name="engine"/> once it is replaced.
		/// </remarks>
		/// <param name="engine">The engine replaced. One that holds no lock, such as the empty log Weevil shows before a log opens, hands none over.</param>
		IEngineBuilder Replacing(IEngine engine);

		/// <summary>
		/// Reads the log, and returns the engine that holds it.
		/// </summary>
		/// <remarks>
		/// Weevil never stops another application from writing, renaming or deleting the log (#1247). Nothing reloads by itself.
		/// When a read finds the log changed or gone, Weevil's window says so, and Ctrl+F5 reloads.
		/// The CLI and PowerShell stop with an error that says to run the command again, or keep a result they could finish, with a warning.
		///
		/// The engine locks the log, with <c>&lt;log&gt;.lock</c> beside it, until it is disposed (#1254). When the lock can't be created, for example in a folder Weevil can't write to,
		/// the log opens anyway, and the application log says so.
		/// </remarks>
		/// <exception cref="LogInUseException">
		/// Another instance of Weevil, on this computer or another, or another engine in this process, has the log open. The message names who has it.
		/// </exception>
		IEngine Open();
	}
}
