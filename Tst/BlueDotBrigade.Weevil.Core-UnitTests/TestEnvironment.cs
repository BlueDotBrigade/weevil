﻿namespace BlueDotBrigade.Weevil
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using BlueDotBrigade.DatenLokator.TestTools.Configuration;

	[TestClass]
	public class TestEnvironment
	{
		[AssemblyInitialize]
		public static void Setup(TestContext context)
		{
			Console.WriteLine("Test environment is being prepared...");
			Lokator.Get().Setup();
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