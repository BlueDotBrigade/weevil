namespace BlueDotBrigade.Weevil
{
	using System;
	using System.IO;
	using System.Reflection;

	public static class EnvironmentHelper
	{
		public static string GetExecutableDirectory()
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}

		public static string GetBinDirectory()
		{
			var executableDirectory = GetExecutableDirectory();

			int indexOfBin = executableDirectory.IndexOf(@"\bin\", StringComparison.InvariantCultureIgnoreCase);
			var binDirectory = executableDirectory.Substring(0, indexOfBin + 1);

			return binDirectory;
		}

		public static string GetProjectDirectory()
		{
			var projectDirectory = Directory.GetParent(GetBinDirectory()).FullName;
			return projectDirectory;
		}

		/// <summary>
		/// Returns the fully qualified path of the solution directory.
		/// </summary>
		/// <remarks>
		/// Method assumes that all project directories are nested 2 levels deep. For example:
		/// <code>c:\Code\MySolution\Src\MyProject</code>
		/// </remarks>
		public static string GetSolutionDirectory()
		{
			var solutionDirectory = Directory.GetParent(GetProjectDirectory()).Parent.FullName;
			return solutionDirectory;
		}
	}
}
