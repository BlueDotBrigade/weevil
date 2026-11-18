namespace BlueDotBrigade.Weevil
{
	using System;
	using System.IO;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Helpers for callers of <see cref="IEngine"/>.
	/// </summary>
	public static class EngineExtensions
	{
		private const string RenamedOrReplaced = "The log was renamed, deleted or replaced while Weevil read it.";

		/// <summary>
		/// Runs <paramref name="work"/>, which reads the log, once, for a command that runs once and stops, such as the CLI or a PowerShell cmdlet (#1247).
		/// Nothing is reloaded or retried: the analyst runs the command again.
		/// </summary>
		/// <param name="warn">
		/// Told when the log was renamed, deleted or replaced, but <paramref name="work"/> still read all it needed, for example in RAM mode.
		/// The result is then returned, as read.
		/// </param>
		/// <exception cref="LogFileChangedException">
		/// A read failed because the log was renamed, deleted or replaced. The message says so, and to run the command again.
		/// </exception>
		public static T RunOnce<T>(this IEngine engine, Func<T> work, Action<string> warn)
		{
			ArgumentNullException.ThrowIfNull(engine);
			ArgumentNullException.ThrowIfNull(work);
			ArgumentNullException.ThrowIfNull(warn);

			T result;

			try
			{
				result = work();
			}
			catch (IOException exception) when (exception is LogFileChangedException || IsRenamedOrReplaced(engine))
			{
				Log.Default.Write(LogSeverityType.Warning, exception, $"{RenamedOrReplaced} The command stops. Path={engine.SourceFilePath}");
				throw new LogFileChangedException($"{RenamedOrReplaced} Run the command again.", exception);
			}

			if (IsRenamedOrReplaced(engine))
			{
				Log.Default.Write(LogSeverityType.Warning, $"{RenamedOrReplaced} The results are kept. Path={engine.SourceFilePath}");
				warn($"{RenamedOrReplaced} The results come from the log as Weevil read it.");
			}

			return result;
		}

		/// <summary>
		/// The log is gone from its path, or shorter than Weevil read it. A log that only grew is the same log.
		/// </summary>
		private static bool IsRenamedOrReplaced(IEngine engine)
		{
			var log = new FileInfo(engine.SourceFilePath);
			return !log.Exists || log.Length < engine.Metrics.FileSize;
		}
	}
}
