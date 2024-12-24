namespace BlueDotBrigade.Weevil.Gui.Configuration.Reqnroll
{
	using System.Windows;
	using BlueDotBrigade.DatenLokator.TestTools.Configuration;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.TestTools.Configuration.Reqnroll;

	/// <summary>
	/// Represents Reqnroll events that execute before & after every test run.
	/// </summary>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/hooks.html">Reqnroll: Hooks</seealso>
	[Binding]
	internal class TestRunHooks
	{
		[BeforeTestRun(Order = Constants.AlwaysFirst)]
		public static void Setup(ITestRunnerManager testRunnerManager)
		{
			Log.Default.Write(LogSeverityType.Debug, "Reqnroll test environment is being setup...");

			// By default `System.Windows.Application.Current` is not initialized by MS Test.
			if (Application.Current == null)
			{
				// Required by FilterViewModel for managing resources: Application.Current.Resources["TextFontSize"]
				// ... Without the following line, automated tests may may fail.
				new Application();
			}

			Lokator
				.Get()
				.UsingDefaultFileName("Droid.log")
				.Setup();

			Log.Default.Write(LogSeverityType.Information, "Reqnroll test environment has been setup.");
		}

		[AfterTestRun(Order = Constants.AlwaysLast)]
		public static void Teardown(ITestRunnerManager testRunnerManager)
		{
			Log.Default.Write(LogSeverityType.Debug, "Reqnroll test environment is being torn down...");

			Lokator
				.Get()
				.TearDown();

			Log.Default.Write(LogSeverityType.Information, "Reqnroll test environment has been torn down.");
		}
	}
}
