namespace BlueDotBrigade.Weevil.Common
{
	using System;
	using System.Collections.Generic;
	using System.IO;
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
			// The library has a bug where it replaces forward slashes with backslashes
			// This workaround explicitly provides the correct path
			var assemblyLocation = Assembly.GetExecutingAssembly().Location;
			var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
			var projectDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", ".."));
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

		[AssemblyCleanup]
		public static void Teardown()
		{
			Console.WriteLine("Test environment is being cleaned up...");
			Lokator.Get().TearDown();
			Console.WriteLine("Test environment preparation is complete.");
		}
	}
}