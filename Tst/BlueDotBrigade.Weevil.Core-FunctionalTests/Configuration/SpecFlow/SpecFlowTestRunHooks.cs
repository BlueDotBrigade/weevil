namespace BlueDotBrigade.Weevil.Configuration.SpecFlow
{
	using BlueDotBrigade.Weevil.Diagnostics;

	/// <summary>
	/// Represents SpecFlow events that execute before & after every test run.
	/// </summary>
	/// <seealso href="https://docs.specflow.org/projects/specflow/en/latest/Bindings/Hooks.html">SpecFlow: Hooks</seealso>
	internal class SpecFlowTestRunHooks
	{
		[BeforeTestRun(Order = Constants.AlwaysFirst)]
		public static void Setup(TestRunnerManager testRunnerManager, ITestRunner testRunner)
		{
			Log.Default.Write(LogSeverityType.Debug, "SpecFlow test environment is being setup...");

			InputData.Setup();

			Log.Default.Write(LogSeverityType.Information, "SpecFlow test environment has been setup.");
		}

		[AfterTestRun(Order = Constants.AlwaysLast)]
		public static void Teardown(TestRunnerManager testRunnerManager, ITestRunner testRunner)
		{
			Log.Default.Write(LogSeverityType.Debug, "SpecFlow test environment is being torn down...");

			InputData.Teardown();

			Log.Default.Write(LogSeverityType.Information, "SpecFlow test environment has been torn down.");
		}
	}
}
