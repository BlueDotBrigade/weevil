namespace BlueDotBrigade.Weevil.Configuration.Reqnroll
{
	using BlueDotBrigade.Weevil.TestingTools.Configuration.Reqnroll;

	/// <summary>
	/// Represents Reqnroll events that execute before & after every Gherkin scenario.
	/// </summary>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/hooks.html">Reqnroll: Hooks</seealso>
	[Binding]
	internal class ScenarioHooks
	{
		public ScenarioHooks()
		{
		}

		[BeforeScenario(Order = Constants.AlwaysFirst)]
		public static void OnBeforeScenario(ScenarioContext scenario)
		{
			//scenario["state"] = new object();
		}

		[BeforeScenarioBlock(Order = Constants.AlwaysFirst)]
		public static void OnBeforeGivenBlock(ScenarioContext scenario)
		{
			if (scenario?.CurrentScenarioBlock == ScenarioBlock.Given)
			{
				// nothing to do
			}
		}

		[AfterScenarioBlock(Order = Constants.AlwaysLast)]
		public static void OnAfterGivenBlock(ScenarioContext scenario)
		{
			// nothing to do
		}

		[AfterScenario(Order = Constants.AlwaysLast)]
		public static void OnAfterScenario(ScenarioContext scenario)
		{
			// nothing to do
		}
	}
}