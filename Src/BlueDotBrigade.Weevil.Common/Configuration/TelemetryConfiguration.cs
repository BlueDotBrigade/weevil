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


		/// <summary>
		/// Returns the telemetry database connection string, or an empty string when no value is configured.
		/// </summary>
		public static string GetConnectionString()
		{
			return LoadConnectionStringFromRegistry();
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

	}
}
