namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Diagnostics;

	public class WindowsProcess
	{
		public static void Start(WindowsProcessType type, string[] args)
		{
			Start(type, GetArgumentsString(args));
		}

		public static void Start(WindowsProcessType type, string args)
		{
			var processPath = string.Empty;

			switch (type)
			{
				case WindowsProcessType.FileExplorer:
					processPath = Environment.ExpandEnvironmentVariables(Environment.Is64BitOperatingSystem ? "%windir%\\SysWOW64" : "%windir%\\System32");
					processPath += @"\explorer.exe";
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(type), "The provided type is not supported.");
			}

			var startInformation = new ProcessStartInfo
			{
				FileName = processPath,
				Arguments = args
			};
			Process.Start(startInformation);
		}

		private static string GetArgumentsString(string[] args)
		{
			var result = string.Empty;

			for (var i = 0; i < args.Length; i++)
			{
				result += args[i] + " ";
			}

			return result.TrimEnd();
		}
	}
}
