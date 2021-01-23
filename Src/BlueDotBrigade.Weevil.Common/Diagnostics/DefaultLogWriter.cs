namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text;

	internal class DefaultLogWriter : ILogWriter
	{
		private static readonly IDictionary NoMetadata = new Hashtable();

		private const LogSeverityType DefaultSeverity = LogSeverityType.Debug;

		private const string DefaultExceptionMessage = "An unexpected exception has been raised.";

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
			Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}", severity, message, Serialize(metadata)));
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
			Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\t{3}", severity, message, Serialize(metadata), exception));
		}

		internal static string Serialize(IDictionary metadata)
		{
			var result = new StringBuilder();

			if (metadata != null)
			{
				foreach (var key in metadata.Keys)
				{
					var safeValue = metadata[key] ?? "NULL";

					result.AppendFormat(CultureInfo.InvariantCulture, "{0}={1};", key, safeValue);
				}
			}

			return result.ToString();
		}
	}
}