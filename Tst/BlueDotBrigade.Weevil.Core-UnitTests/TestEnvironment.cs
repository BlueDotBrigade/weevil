namespace BlueDotBrigade.Weevil
{
	using System;
	using System.IO;
	using System.Reflection;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using BlueDotBrigade.DatenLokator.TestTools.Configuration;

	[TestClass]
	public class TestEnvironment
	{
		private const BindingFlags PrivateInstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;

		[AssemblyInitialize]
		public static void Setup(TestContext context)
		{
			Console.WriteLine("Test environment is being prepared...");
			
			// Workaround for DatenLokator v2.3.0 bug on Linux/Unix systems
			// The library incorrectly handles absolute paths on non-Windows platforms:
			// 1. It prefixes Unix absolute paths with a backslash (\home/... instead of /home/...)
			// 2. When combining paths, it treats absolute paths as relative paths
			// 
			// This bug only affects non-Windows platforms where Path.DirectorySeparatorChar == '/'
			// The CI currently only runs on Windows, so this bug was never encountered before.
			//
			// Solution: Use reflection to manually set correct paths in the Coordinator and FileManagementStrategy
			if (Path.DirectorySeparatorChar == '/')
			{
				try
				{
					// Find the test project directory containing .Daten folder
					var assemblyLocation = typeof(TestEnvironment).Assembly.Location;
					var testProjectDir = Path.GetDirectoryName(assemblyLocation);
					
					Console.WriteLine($"Assembly location: {assemblyLocation}");
					Console.WriteLine($"Starting search from: {testProjectDir}");
					
					if (testProjectDir == null)
					{
						throw new InvalidOperationException("Could not determine assembly directory");
					}
					
					// Navigate up from bin/[Platform]/[Configuration] to project root
					while (!Directory.Exists(Path.Combine(testProjectDir, ".Daten")))
					{
						var parent = Path.GetDirectoryName(testProjectDir);
						if (parent == testProjectDir || parent == null)
						{
							throw new InvalidOperationException($"Could not find .Daten directory starting from: {assemblyLocation}");
						}
						testProjectDir = parent;
					}
					
					Console.WriteLine($"Found .Daten directory at: {testProjectDir}");
					
					// Get the Lokator singleton instance
					// The coordinator is created when we call Get(), but not set up yet
					var lokator = Lokator.Get();
					var lokatorType = lokator.GetType();
					
					// Access the private _coordinator field
					var coordinatorField = lokatorType.GetField("_coordinator", PrivateInstanceFlags);
					
					if (coordinatorField == null)
					{
						throw new InvalidOperationException("Could not find _coordinator field in Lokator");
					}
					
					var coordinator = coordinatorField.GetValue(lokator);
					if (coordinator == null)
					{
						throw new InvalidOperationException("Coordinator is null");
					}
					
					var coordinatorType = coordinator.GetType();
					
					// 1. Set _rootDirectoryPath in the coordinator
					var rootPathField = coordinatorType.GetField("_rootDirectoryPath", PrivateInstanceFlags);
					
					if (rootPathField == null)
					{
						throw new InvalidOperationException("Could not find _rootDirectoryPath field in Coordinator");
					}
					
					rootPathField.SetValue(coordinator, testProjectDir);
					Console.WriteLine($"Set root directory path to: {testProjectDir}");
					
					// 2. Get the file management strategy and fix its root path
					var fileStrategyField = coordinatorType.GetField("_fileManagementStrategy", PrivateInstanceFlags);
					
					if (fileStrategyField != null)
					{
						var fileStrategy = fileStrategyField.GetValue(coordinator);
						if (fileStrategy != null)
						{
							var fileStrategyType = fileStrategy.GetType();
							
							// Set the _rootDirectoryPath in the strategy
							var strategyRootPathField = fileStrategyType.GetField("_rootDirectoryPath", PrivateInstanceFlags);
							
							if (strategyRootPathField != null)
							{
								strategyRootPathField.SetValue(fileStrategy, testProjectDir);
								Console.WriteLine($"Set strategy root directory path to: {testProjectDir}");
							}
							
							// Set the _isSetup flag in the strategy
							var strategyIsSetupField = fileStrategyType.GetField("_isSetup", PrivateInstanceFlags);
							
							if (strategyIsSetupField != null)
							{
								strategyIsSetupField.SetValue(fileStrategy, true);
								Console.WriteLine("Marked strategy as setup");
							}
						}
					}
					
					// 3. Set _isSetup flag in coordinator
					var isSetupField = coordinatorType.GetField("_isSetup", PrivateInstanceFlags);
					
					if (isSetupField != null)
					{
						isSetupField.SetValue(coordinator, true);
						Console.WriteLine("Marked coordinator as setup");
					}
					
					Console.WriteLine("Successfully worked around DatenLokator bug");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error during DatenLokator workaround: {ex.Message}");
					Console.WriteLine($"Stack trace: {ex.StackTrace}");
					throw new InvalidOperationException("Failed to initialize test environment due to DatenLokator bug on Linux", ex);
				}
			}
			else
			{
				// On Windows, the DatenLokator library works correctly
				Lokator.Get().Setup();
			}
			
			Console.WriteLine("Test environment preparation is complete.");
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