namespace BlueDotBrigade.Weevil.PowerShell
{
	using BlueDotBrigade.DatenLokator.TestTools.Configuration;

	[TestClass]
	public class TestEnvironment
	{
		[AssemblyInitialize]
		public static void Setup(TestContext context)
		{
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
