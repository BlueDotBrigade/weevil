namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Text;

	internal class DefaultLogWriter : ILogWriter
	{
		private static readonly IDictionary<string, object> NoMetadata = new Dictionary<string, object>();

		private const LogSeverityType DefaultSeverity = LogSeverityType.Debug;

		private const string DefaultExceptionMessage = "An unexpected exception has been raised.";

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

		public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata)
		{
			Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\t{3}", severity, message, Serialize(metadata), exception));
		}

		internal static string Serialize(IEnumerable<KeyValuePair<string, object>> metadata)
		{
			var result = new StringBuilder();

			if (metadata != null)
			{
				IEnumerable<KeyValuePair<string, object>> keyValuePairs = metadata as KeyValuePair<string, object>[] ?? metadata.ToArray();

				foreach (var item in keyValuePairs)
				{
					result.AppendFormat(CultureInfo.InvariantCulture, "{0}={1};", item.Key, item.Value ?? "NULL");
				}
			}

			return result.ToString();
		}
	}
}