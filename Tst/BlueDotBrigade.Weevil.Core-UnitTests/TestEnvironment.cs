namespace BlueDotBrigade.Weevil
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using BlueDotBrigade.DatenLokator.TestTools.Configuration;

	[TestClass]
	public class TestEnvironment
	{
		private const string DatenDirectoryName = ".Daten";
		
		[AssemblyInitialize]
		public static void Setup(TestContext context)
		{
			Console.WriteLine("Test environment is being prepared...");
			
			// Workaround for DatenLokator v2.3.0 bug on Linux/Unix systems.
			// The library incorrectly replaces forward slashes with backslashes,
			// causing paths like "/home/..." to become "\home\..." which don't exist.
			// We provide the correct root path via TestContext to bypass the buggy path construction.
			var projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
			var binIndex = projectDirectory.IndexOf(
				$"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}",
				StringComparison.OrdinalIgnoreCase);
			
			if (binIndex > 0)
			{
				projectDirectory = projectDirectory.Substring(0, binIndex);
			}
			
			var rootDirectoryPath = System.IO.Path.Combine(projectDirectory, DatenDirectoryName);
			context.Properties["DatenLokatorRootPath"] = rootDirectoryPath;
			
			Lokator.Get()
				.UsingTestContext(context.Properties)
				.Setup();
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