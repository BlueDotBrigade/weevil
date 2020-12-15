namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections;
	using NLog;
	using NLog.Fluent;

	/// <summary>
	/// Represents an adapter that facilitates writing to NLog.
	/// </summary>
	/// <seealso href="https://nlog-project.org/">NLog Project</seealso>
	/// <seealso href="https://github.com/NLog/NLog/wiki/Fluent-API">NLog Fluent API</seealso>
	/// <seealso href="https://stackoverflow.com/a/7486163/949681">Creating an NLog wrapper</seealso>
	public sealed class NLogWriter : ILogWriter
	{
		private static readonly Logger LogWriter = NLog.LogManager.GetCurrentClassLogger();

		private static readonly IDictionary NoMetadata = new Hashtable();

		private static readonly LogSeverityType DefaultSeverity = LogSeverityType.Debug;

		private static readonly string DefaultExceptionMessage = "An unexpected exception has been raised.";

		public void Write(string message)
		{
			Write(DefaultSeverity, message, NoMetadata);
		}

		public void Write(string message, IDictionary metadata)
		{
			Write(DefaultSeverity, message, metadata);
		}

		public void Write(LogSeverityType severity, string message)
		{
			Write(severity, message, NoMetadata);
		}

		public void Write(LogSeverityType severity, string message, IDictionary metadata)
		{
			switch (severity)
			{
				case LogSeverityType.Trace:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Trace().Message(message).Properties(metadata).LogEventInfo);
					break;

				case LogSeverityType.Information:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Info().Message(message).Properties(metadata).LogEventInfo);
					break;

				case LogSeverityType.Warning:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Warn().Message(message).Properties(metadata).LogEventInfo);
					break;

				case LogSeverityType.Error:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Error().Message(message).Properties(metadata).LogEventInfo);
					break;

				case LogSeverityType.Critical:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Fatal().Message(message).Properties(metadata).LogEventInfo);
					break;

				case LogSeverityType.Debug:
				default:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Debug().Message(message).Properties(metadata).LogEventInfo);
					break;
			}
		}

		public void Write(LogSeverityType severity, Exception exception)
		{
			Write(severity, exception, DefaultExceptionMessage, NoMetadata);
		}

		public void Write(LogSeverityType severity, Exception exception, string message)
		{
			Write(severity, exception, message, NoMetadata);
		}

		public void Write(LogSeverityType severity, Exception exception, string message, IDictionary metadata)
		{
			switch (severity)
			{
				case LogSeverityType.Trace:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Trace().Message(message).Properties(metadata).Exception(exception).LogEventInfo);
					break;

				case LogSeverityType.Information:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Debug().Message(message).Properties(metadata).Exception(exception).LogEventInfo);
					break;

				case LogSeverityType.Warning:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Warn().Message(message).Properties(metadata).Exception(exception).LogEventInfo);
					break;

				case LogSeverityType.Error:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Error().Message(message).Properties(metadata).Exception(exception).LogEventInfo);
					break;

				case LogSeverityType.Critical:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Fatal().Message(message).Properties(metadata).Exception(exception).LogEventInfo);
					break;

				case LogSeverityType.Debug:
				default:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.Debug().Message(message).Properties(metadata).Exception(exception).LogEventInfo);
					break;
			}
		}
	}
}