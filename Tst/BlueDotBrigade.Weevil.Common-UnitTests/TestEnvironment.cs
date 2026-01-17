namespace BlueDotBrigade.Weevil.Common
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using BlueDotBrigade.DatenLokator.TestTools.Configuration;

	[TestClass]
	public class TestEnvironment
	{
		[AssemblyInitialize]
		public static void Setup(TestContext context)
		{
			Console.WriteLine("Test environment is being prepared...");
			
			// Fix for DatenLokator path separator issue on Linux
			// The library (v2.3.0) has a bug where it replaces forward slashes with backslashes
			// in its ExecutingDirectory property, causing test initialization to fail on Linux.
			// This workaround explicitly provides the correct path by searching upward from
			// the assembly location for the project directory (identified by the .csproj file).
			var assemblyLocation = Assembly.GetExecutingAssembly().Location;
			var projectDirectory = FindProjectDirectory(Path.GetDirectoryName(assemblyLocation));
			var datenDirectory = Path.Combine(projectDirectory, ".Daten");
			
			var properties = new Dictionary<string, object>
			{
				{ "DatenLokatorRootPath", datenDirectory }
			};
			
			Lokator.Get()
				.UsingTestContext(properties)
				.Setup();
			
			Console.WriteLine("Test environment preparation is complete.");
		}

		private static string FindProjectDirectory(string startDirectory)
		{
			var currentDirectory = new DirectoryInfo(startDirectory);
			
			while (currentDirectory != null)
			{
				// Look for a .csproj file to identify the project root
				if (currentDirectory.GetFiles("*.csproj").Any())
				{
					return currentDirectory.FullName;
				}
				currentDirectory = currentDirectory.Parent;
			}
			
			throw new InvalidOperationException(
				$"Could not find project directory starting from: {startDirectory}");
		}

		[AssemblyCleanup]
		public static void Teardown()
		{
			Console.WriteLine("Test environment is being cleaned up...");
			Lokator.Get().TearDown();
			Console.WriteLine("Test environment preparation is complete.");
		}
	}
}