namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Diagnostics;
	using System.IO;

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
					var startInformation = new ProcessStartInfo
					{
						FileName = processPath,
						Arguments = args
					};
					Process.Start(startInformation);
					break;

				case WindowsProcessType.DefaultApplication:
					var processDetails = new ProcessStartInfo
					{
						WorkingDirectory = Path.GetDirectoryName(args),
						FileName = args,
						// Needed to avoid .NET Core runtime error
						// ... "The specified executable is not a valid application for this OS platform"
						UseShellExecute = true,
					};
					Process.Start(processDetails);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(type), "The provided type is not supported.");
			}


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
