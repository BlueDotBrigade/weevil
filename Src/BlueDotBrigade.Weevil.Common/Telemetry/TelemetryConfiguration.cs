namespace BlueDotBrigade.Weevil
{
	using System;
	using System.IO;
	using System.Security;
	using Microsoft.Win32;

	public static class TelemetryConfiguration
	{
		private const string RegistryPath = @"Software\BlueDotBrigade\Weevil";
		private const string RegistryValueName = "TelemetryEnabled";

		public static bool IsEnabled()
		{
			return LoadIsEnabledFromRegistry();
		}

		internal static bool ParseEnabledValue(string rawValue)
		{
			if (string.IsNullOrWhiteSpace(rawValue))
			{
				return true;
			}

			if (bool.TryParse(rawValue, out var isEnabled))
			{
				return isEnabled;
			}

			if (int.TryParse(rawValue, out var numericValue))
			{
				return numericValue != 0;
			}

			return true;
		}

		private static bool LoadIsEnabledFromRegistry()
		{
			if (!OperatingSystem.IsWindows())
			{
				return true;
			}

			try
			{
				using var registryKey = Registry.CurrentUser.OpenSubKey(RegistryPath);
				var rawValue = registryKey?.GetValue(RegistryValueName)?.ToString();

				return ParseEnabledValue(rawValue);
			}
			catch (Exception exception) when (
				exception is SecurityException ||
				exception is UnauthorizedAccessException ||
				exception is IOException ||
				exception is PlatformNotSupportedException)
			{
				return true;
			}
		}
	}
}
