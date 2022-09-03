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

		private static readonly IDictionary NoMetadata = new Hashtable();

		private static readonly IEnumerable<KeyValuePair<string, object>> NoProperties
			= Array.Empty<KeyValuePair<string, object>>();

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
			IEnumerable<KeyValuePair<string, object>> properties 
				= metadata as IEnumerable<KeyValuePair<string, object>> ?? NoProperties;

			switch (severity)
			{
				case LogSeverityType.Trace:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForTraceEvent().Message(message).Properties(properties).LogEvent);
					break;

				case LogSeverityType.Information:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForInfoEvent().Message(message).Properties(properties).LogEvent);
					break;

				case LogSeverityType.Warning:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForWarnEvent().Message(message).Properties(properties).LogEvent);
					break;

				case LogSeverityType.Error:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForErrorEvent().Message(message).Properties(properties).LogEvent);
					break;

				case LogSeverityType.Critical:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForFatalEvent().Message(message).Properties(properties).LogEvent);
					break;

				case LogSeverityType.Debug:
				default:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForDebugEvent().Message(message).Properties(properties).LogEvent);
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
			IEnumerable<KeyValuePair<string, object>> properties 
				= metadata as IEnumerable<KeyValuePair<string, object>> ?? NoProperties;

			switch (severity)
			{
				case LogSeverityType.Trace:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForTraceEvent().Message(message).Properties(properties).Exception(exception).LogEvent);
					break;

				case LogSeverityType.Information:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForInfoEvent().Message(message).Properties(properties).Exception(exception).LogEvent);
					break;

				case LogSeverityType.Warning:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForWarnEvent().Message(message).Properties(properties).Exception(exception).LogEvent);
					break;

				case LogSeverityType.Error:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForErrorEvent().Message(message).Properties(properties).Exception(exception).LogEvent);
					break;

				case LogSeverityType.Critical:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForFatalEvent().Message(message).Properties(properties).Exception(exception).LogEvent);
					break;

				case LogSeverityType.Debug:
				default:
					LogWriter.Log(
						typeof(NLogWriter),
						LogWriter.ForDebugEvent().Message(message).Properties(properties).Exception(exception).LogEvent);
					break;
			}
		}
	}
}