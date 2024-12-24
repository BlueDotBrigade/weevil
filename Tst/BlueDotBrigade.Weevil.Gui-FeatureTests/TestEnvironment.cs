

namespace BlueDotBrigade.Weevil.Gui
{
	using System.Windows;
	using BlueDotBrigade;
	using BlueDotBrigade.Weevil;
	using BlueDotBrigade.Weevil.Gui;
	using BlueDotBrigade.Weevil.Gui.Configuration;
	using BlueDotBrigade.Weevil.Gui.Configuration.Reqnroll;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.TestTools.Configuration.Reqnroll;
	using BlueDotBrigade.DatenLokator.TestTools.Configuration;

	/// <summary>
	/// Represents Reqnroll events that execute before & after every test run.
	/// </summary>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/hooks.html">Reqnroll: Hooks</seealso>
	[Binding]
	internal class TestEnvironment
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
