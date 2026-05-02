namespace BlueDotBrigade.Weevil.Configuration
{
	using System;
	using System.IO;
	using System.Security;
	using Microsoft.Win32;

	public static class TelemetryConfiguration
	{
		private const string RegistryPath = @"Software\BlueDotBrigade\Weevil";
		private const string ConnectionStringValueName = "TelemetryConnectionString";
		private const string SourceValueName = "TelemetrySource";


		/// <summary>
		/// Returns the telemetry database connection string, or an empty string when no value is configured.
		/// </summary>
		public static string GetConnectionString()
		{
			return LoadConnectionStringFromRegistry();
		}

		/// <summary>
		/// Returns the telemetry source identifier for the current installation.
		/// </summary>
		public static string GetSource()
		{
			return LoadSourceFromRegistry();
		}

		private static string LoadConnectionStringFromRegistry()
		{
			if (!OperatingSystem.IsWindows())
			{
				return string.Empty;
			}

			try
			{
				using var registryKey = Registry.CurrentUser.OpenSubKey(RegistryPath);
				return registryKey?.GetValue(ConnectionStringValueName)?.ToString() ?? string.Empty;
			}
			catch (Exception exception) when (
				exception is SecurityException ||
				exception is UnauthorizedAccessException ||
				exception is IOException ||
				exception is PlatformNotSupportedException)
			{
				return string.Empty;
			}
		}

		private static string LoadSourceFromRegistry()
		{
			if (!OperatingSystem.IsWindows())
			{
				return "unknown";
			}

			try
			{
				using var registryKey = Registry.CurrentUser.OpenSubKey(RegistryPath);
				var source = registryKey?.GetValue(SourceValueName)?.ToString();
				return string.IsNullOrWhiteSpace(source) ? "unknown" : source;
			}
			catch (Exception exception) when (
				exception is SecurityException ||
				exception is UnauthorizedAccessException ||
				exception is IOException ||
				exception is PlatformNotSupportedException)
			{
				return "unknown";
			}
		}
	}
}
