namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections;
	using System.ComponentModel;

	public interface ILogWriter
	{
		void Write([Localizable(false)] string message);
		void Write(string message, IDictionary metadata);

		void Write(LogSeverityType severity, [Localizable(false)] string message);
		void Write(LogSeverityType severity, [Localizable(false)] string message, IDictionary metadata);

		void Write(LogSeverityType severity, Exception exception);
		void Write(LogSeverityType severity, Exception exception, [Localizable(false)] string message);
		void Write(LogSeverityType severity, Exception exception, [Localizable(false)] string message, IDictionary metadata);
	}
}