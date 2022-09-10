namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	public interface ILogWriter
	{
		void Write([Localizable(false)] string message);
		void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata);

		void Write(LogSeverityType severity, [Localizable(false)] string message);
		void Write(LogSeverityType severity, [Localizable(false)] string message, IEnumerable<KeyValuePair<string, object>> metadata);

		void Write(LogSeverityType severity, Exception exception);
		void Write(LogSeverityType severity, Exception exception, [Localizable(false)] string message);
		void Write(LogSeverityType severity, Exception exception, [Localizable(false)] string message, IEnumerable<KeyValuePair<string, object>> metadata);
	}
}