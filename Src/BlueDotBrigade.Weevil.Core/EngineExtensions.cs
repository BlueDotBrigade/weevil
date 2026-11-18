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
			ArgumentNullException.ThrowIfNull(warn);

			return RunOnce(engine, work, isResultComplete: true, warn);
		}

		/// <summary>
		/// As <see cref="RunOnce{T}(IEngine, Func{T}, Action{string})"/>, for a result that still reads the log,
		/// such as records returned in disk mode: a log renamed, deleted or replaced meanwhile is an error, even when <paramref name="work"/> finished.
		/// </summary>
		/// <exception cref="LogFileChangedException">
		/// The log was renamed, deleted or replaced. The message says so, and to run the command again.
		/// </exception>
		public static T RunOnce<T>(this IEngine engine, Func<T> work)
		{
			return RunOnce(engine, work, isResultComplete: false, warn: null);
		}

		private static T RunOnce<T>(IEngine engine, Func<T> work, bool isResultComplete, Action<string> warn)
		{
			ArgumentNullException.ThrowIfNull(engine);
			ArgumentNullException.ThrowIfNull(work);

			T result;

			try
			{
				result = work();
			}
			catch (IOException exception) when (exception is LogFileChangedException ||
			                                     (exception is FileNotFoundException or DirectoryNotFoundException && IsRenamedOrReplaced(engine)))
			{
				throw Stop(engine, exception);
			}

			if (IsRenamedOrReplaced(engine))
			{
				if (!isResultComplete)
				{
					throw Stop(engine, null);
				}

				Log.Default.Write(LogSeverityType.Warning, $"{RenamedOrReplaced} The results are kept. Path={engine.SourceFilePath}");
				warn($"{RenamedOrReplaced} The results come from the log as Weevil read it.");
			}

			return result;
		}

		private static LogFileChangedException Stop(IEngine engine, Exception readFailure)
		{
			var message = $"{RenamedOrReplaced} The command stops. Path={engine.SourceFilePath}";

			if (readFailure is null)
			{
				Log.Default.Write(LogSeverityType.Warning, message);
			}
			else
			{
				Log.Default.Write(LogSeverityType.Warning, readFailure, message);
			}

			return new LogFileChangedException($"{RenamedOrReplaced} Run the command again.", readFailure);
		}

		/// <summary>
		/// The log is gone from its path, or shorter than Weevil read it.
		/// </summary>
		/// <remarks>
		/// A log that only grew is the same log. So is one replaced by a longer one, which this cannot tell apart.
		/// </remarks>
		private static bool IsRenamedOrReplaced(IEngine engine)
		{
			var log = new FileInfo(engine.SourceFilePath);
			return !log.Exists || log.Length < engine.Metrics.FileSize;
		}
	}
}
