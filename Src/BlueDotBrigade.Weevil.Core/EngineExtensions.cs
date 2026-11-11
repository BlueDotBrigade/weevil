namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Helpers for callers of <see cref="IEngine"/>.
	/// </summary>
	public static class EngineExtensions
	{
		/// <summary>
		/// Runs <paramref name="work"/>. If a read finds the log changed, reloads it and runs <paramref name="work"/> once more (#1028).
		/// </summary>
		/// <remarks>
		/// For a caller that runs one command and stops, such as the CLI or a PowerShell cmdlet, as Doc/Design/DiskMode.md asks.
		/// Only a <see cref="LogFileChangedException"/> is retried, and only once. Any other exception, or a second change, reaches the caller,
		/// and so does an exception from <see cref="IEngine.Reload"/>. <paramref name="work"/> must read the records from the engine it is given, because Reload replaces them.
		/// </remarks>
		/// <exception cref="LogFileChangedException">The log changed again during the second run.</exception>
		public static T RunReloadingOnChange<T>(this IEngine engine, Func<IEngine, T> work)
		{
			ArgumentNullException.ThrowIfNull(engine);
			ArgumentNullException.ThrowIfNull(work);

			try
			{
				return work(engine);
			}
			catch (LogFileChangedException exception)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					$"The log changed while it was read, so it is reloaded and read once more. Path={exception.Path}");

				engine.Reload();

				return work(engine);
			}
		}
	}
}
