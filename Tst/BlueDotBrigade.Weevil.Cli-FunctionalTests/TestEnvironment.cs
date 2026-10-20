namespace BlueDotBrigade.Weevil.Cli
{
	using BlueDotBrigade.DatenLokator.TestTools.Configuration;

	[TestClass]
	public class TestEnvironment
	{
		[AssemblyInitialize]
		public static void Setup(TestContext context)
		{
			// Installing Weevil turns telemetry on for the whole machine. The tests run real commands, so they must not record sessions.
			Environment.SetEnvironmentVariable("WEEVIL_TELEMETRY_ENABLED", null);

			Lokator
				.Get()
				.Setup();
		}

		[AssemblyCleanup]
		public static void Teardown()
		{
			Lokator.Get().TearDown();
		}
	}
}
