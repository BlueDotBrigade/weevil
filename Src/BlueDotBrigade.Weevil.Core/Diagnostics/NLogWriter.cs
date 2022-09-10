namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
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

		private static readonly IDictionary<string, object> NoMetadata = new Dictionary<string, object>();

		private static readonly LogSeverityType DefaultSeverity = LogSeverityType.Debug;

		private static readonly string DefaultExceptionMessage = "An unexpected exception has been raised.";

		private NLog.LogLevel ResolveLogLevel(LogSeverityType severity)
		{
			switch (severity)
			{
				case LogSeverityType.Trace: return NLog.LogLevel.Trace;
				case LogSeverityType.Information: return NLog.LogLevel.Info;
				case LogSeverityType.Warning: return NLog.LogLevel.Warn;
				case LogSeverityType.Error: return NLog.LogLevel.Error;
				case LogSeverityType.Critical: return NLog.LogLevel.Fatal;
				case LogSeverityType.Debug:
				default:
					return NLog.LogLevel.Debug;
			}
		}

		public void Write(string message)
		{
			Write(DefaultSeverity, message, NoMetadata);
		}

		public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata)
		{
			Write(DefaultSeverity, message, metadata);
		}

		public void Write(LogSeverityType severity, string message)
		{
			Write(severity, message, NoMetadata);
		}

		public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata)
		{
			LogLevel logLevel = ResolveLogLevel(severity);
			IEnumerable<KeyValuePair<string, object>> properties = metadata ?? NoMetadata;
			
			LogEventInfo logEvent = LogWriter
				.ForLogEvent(logLevel)
				.Message(message)
				.Properties(properties)
				.LogEvent;

			if (logEvent == null)
			{
				// As per SnakeFoot pointed out, we have to handle an empty `LogEvent` which can occur with NLog v5.
				// ... Beause `LogEvent` property can return null when the LogLevel. Trace is not enabled (skips allocation when not needed).
				// ... And one is not allowed to pass LogEventInfo == null into Logger.Log(...)-method, so now it will throw when LogLevel is not enabled.
				// ... https://github.com/BlueDotBrigade/weevil/pull/352
			}
			else
			{
				LogWriter.Log(typeof(NLogWriter), logEvent);
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

		public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata)
		{
			LogLevel logLevel = ResolveLogLevel(severity);
			IEnumerable<KeyValuePair<string, object>> properties = metadata ?? NoMetadata;
			
			LogEventInfo logEvent = LogWriter
				.ForLogEvent(logLevel)
				.Exception(exception)
				.Message(message)
				.Properties(properties)
				.LogEvent;

			if (logEvent == null)
			{
				// As per SnakeFoot pointed out, we have to handle an empty `LogEvent` which can occur with NLog v5.
				// ... Beause `LogEvent` property can return null when the LogLevel. Trace is not enabled (skips allocation when not needed).
				// ... And one is not allowed to pass LogEventInfo == null into Logger.Log(...)-method, so now it will throw when LogLevel is not enabled.
				// ... https://github.com/BlueDotBrigade/weevil/pull/352
			}
			else
			{
				LogWriter.Log(typeof(NLogWriter), logEvent);
			}
		}
	}
}