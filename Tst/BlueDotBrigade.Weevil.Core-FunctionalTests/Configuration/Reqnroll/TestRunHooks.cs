namespace BlueDotBrigade.Weevil.Configuration.Reqnroll
{
	using BlueDotBrigade.DatenLokator.TestsTools.Configuration;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.TestingTools.Configuration.Reqnroll;

	/// <summary>
	/// Represents Reqnroll events that execute before & after every test run.
	/// </summary>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/hooks.html">Reqnroll: Hooks</seealso>
	[Binding]
	internal class TestRunHooks
	{
		[BeforeTestRun(Order = Constants.AlwaysFirst)]
		public static void Setup(TestRunnerManager testRunnerManager, ITestRunner testRunner)
		{
			Log.Default.Write(LogSeverityType.Debug, "Reqnroll test environment is being setup...");

			Lokator
				.Get()
				.UsingDefaultFileName("GenericBaseline.log")
				.Setup();

			Log.Default.Write(LogSeverityType.Information, "Reqnroll test environment has been setup.");
		}

		[AfterTestRun(Order = Constants.AlwaysLast)]
		public static void Teardown(TestRunnerManager testRunnerManager, ITestRunner testRunner)
		{
			Log.Default.Write(LogSeverityType.Debug, "Reqnroll test environment is being torn down...");

			Lokator
				.Get()
				.TearDown();

			Log.Default.Write(LogSeverityType.Information, "Reqnroll test environment has been torn down.");
		}
	}
}
